using System.Data;
using Microsoft.Data.SqlClient;

namespace NewVK.Data
{
    public sealed class MessagesRepository
    {
        private readonly AppDbConnectionFactory _connectionFactory;

        public MessagesRepository(AppDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IReadOnlyList<MessageDialogListItem>> GetDialogsAsync(int userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT
    c.Id AS ConversationId,
    otherUser.Id AS OtherUserId,
    otherUser.Login AS OtherUserLogin,
    otherUser.FirstName AS OtherUserFirstName,
    otherUser.LastName AS OtherUserLastName,
    lastMessage.Body AS Preview,
    lastMessage.SentAtUtc AS LastMessageAtUtc,
    (
        SELECT COUNT(1)
        FROM dbo.Messages unread
        WHERE unread.ConversationId = c.Id
          AND unread.IsDeleted = 0
          AND unread.SenderUserId <> @UserId
          AND (selfParticipant.LastReadAtUtc IS NULL OR unread.SentAtUtc > selfParticipant.LastReadAtUtc)
    ) AS UnreadCount
FROM dbo.ConversationParticipants selfParticipant
INNER JOIN dbo.Conversations c
    ON c.Id = selfParticipant.ConversationId
   AND c.ConversationType = N'Direct'
INNER JOIN dbo.ConversationParticipants otherParticipant
    ON otherParticipant.ConversationId = c.Id
   AND otherParticipant.UserId <> @UserId
INNER JOIN dbo.Users otherUser
    ON otherUser.Id = otherParticipant.UserId
OUTER APPLY
(
    SELECT TOP (1)
        m.Body,
        m.SentAtUtc
    FROM dbo.Messages m
    WHERE m.ConversationId = c.Id
      AND m.IsDeleted = 0
    ORDER BY m.SentAtUtc DESC, m.Id DESC
) AS lastMessage
WHERE selfParticipant.UserId = @UserId
  AND lastMessage.SentAtUtc IS NOT NULL
ORDER BY lastMessage.SentAtUtc DESC, c.Id DESC;";

            var result = new List<MessageDialogListItem>();

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new MessageDialogListItem
                {
                    ConversationId = reader.GetInt32(reader.GetOrdinal("ConversationId")),
                    OtherUserId = reader.GetInt32(reader.GetOrdinal("OtherUserId")),
                    OtherUserLogin = reader.GetString(reader.GetOrdinal("OtherUserLogin")),
                    OtherUserName = BuildDisplayName(
                        reader.GetString(reader.GetOrdinal("OtherUserLogin")),
                        reader.GetString(reader.GetOrdinal("OtherUserFirstName")),
                        reader.GetString(reader.GetOrdinal("OtherUserLastName"))),
                    Preview = reader.GetString(reader.GetOrdinal("Preview")),
                    LastMessageAtUtc = reader.GetDateTime(reader.GetOrdinal("LastMessageAtUtc")),
                    UnreadCount = reader.GetInt32(reader.GetOrdinal("UnreadCount"))
                });
            }

            return result;
        }

        public async Task<IReadOnlyList<DirectMessageItem>> GetMessagesAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT
    m.Id,
    m.SenderUserId,
    m.Body,
    m.SentAtUtc
FROM dbo.Messages m
INNER JOIN dbo.ConversationParticipants cp
    ON cp.ConversationId = m.ConversationId
   AND cp.UserId = @UserId
WHERE m.ConversationId = @ConversationId
  AND m.IsDeleted = 0
ORDER BY m.SentAtUtc ASC, m.Id ASC;";

            var result = new List<DirectMessageItem>();

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@ConversationId", SqlDbType.Int) { Value = conversationId });
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new DirectMessageItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    SenderUserId = reader.GetInt32(reader.GetOrdinal("SenderUserId")),
                    Body = reader.GetString(reader.GetOrdinal("Body")),
                    SentAtUtc = reader.GetDateTime(reader.GetOrdinal("SentAtUtc"))
                });
            }

            return result;
        }

        public async Task MarkAsReadAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
UPDATE cp
SET LastReadAtUtc = COALESCE(c.LastMessageAtUtc, SYSUTCDATETIME())
FROM dbo.ConversationParticipants cp
INNER JOIN dbo.Conversations c
    ON c.Id = cp.ConversationId
WHERE cp.ConversationId = @ConversationId
  AND cp.UserId = @UserId;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@ConversationId", SqlDbType.Int) { Value = conversationId });
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<SendDirectMessageResult> SendDirectMessageByLoginAsync(
            int senderUserId,
            string recipientLogin,
            string body,
            CancellationToken cancellationToken = default)
        {
            const string recipientSql = @"
SELECT TOP (1)
    Id
FROM dbo.Users
WHERE NormalizedLogin = @NormalizedLogin;";

            const string conversationSql = @"
SELECT TOP (1)
    c.Id
FROM dbo.Conversations c WITH (UPDLOCK, HOLDLOCK)
INNER JOIN dbo.ConversationParticipants cpSender
    ON cpSender.ConversationId = c.Id
   AND cpSender.UserId = @SenderUserId
INNER JOIN dbo.ConversationParticipants cpRecipient
    ON cpRecipient.ConversationId = c.Id
   AND cpRecipient.UserId = @RecipientUserId
WHERE c.ConversationType = N'Direct'
  AND
  (
      SELECT COUNT(1)
      FROM dbo.ConversationParticipants allParticipants
      WHERE allParticipants.ConversationId = c.Id
  ) = 2
ORDER BY c.Id;";

            const string createConversationSql = @"
INSERT INTO dbo.Conversations
(
    ConversationType,
    CreatedByUserId,
    CreatedAtUtc,
    LastMessageAtUtc
)
VALUES
(
    N'Direct',
    @SenderUserId,
    @SentAtUtc,
    @SentAtUtc
);

SELECT CAST(SCOPE_IDENTITY() AS int);";

            const string addParticipantSql = @"
INSERT INTO dbo.ConversationParticipants
(
    ConversationId,
    UserId,
    JoinedAtUtc,
    LastReadAtUtc
)
VALUES
(
    @ConversationId,
    @UserId,
    @SentAtUtc,
    @LastReadAtUtc
);";

            const string insertMessageSql = @"
INSERT INTO dbo.Messages
(
    ConversationId,
    SenderUserId,
    Body,
    SentAtUtc
)
VALUES
(
    @ConversationId,
    @SenderUserId,
    @Body,
    @SentAtUtc
);";

            const string updateConversationSql = @"
UPDATE dbo.Conversations
SET LastMessageAtUtc = @SentAtUtc
WHERE Id = @ConversationId;";

            const string updateSenderReadSql = @"
UPDATE dbo.ConversationParticipants
SET LastReadAtUtc = @SentAtUtc
WHERE ConversationId = @ConversationId
  AND UserId = @SenderUserId;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            try
            {
                int recipientUserId;

                await using (var recipientCommand = new SqlCommand(recipientSql, connection, transaction))
                {
                    AddNVarChar(recipientCommand, "@NormalizedLogin", 50, Normalize(recipientLogin));

                    object? recipientResult = await recipientCommand.ExecuteScalarAsync(cancellationToken);
                    if (recipientResult is null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return SendDirectMessageResult.RecipientNotFound();
                    }

                    recipientUserId = Convert.ToInt32(recipientResult);
                }

                if (recipientUserId == senderUserId)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return SendDirectMessageResult.CannotMessageSelf();
                }

                int? conversationId = null;

                await using (var conversationCommand = new SqlCommand(conversationSql, connection, transaction))
                {
                    conversationCommand.Parameters.Add(new SqlParameter("@SenderUserId", SqlDbType.Int) { Value = senderUserId });
                    conversationCommand.Parameters.Add(new SqlParameter("@RecipientUserId", SqlDbType.Int) { Value = recipientUserId });

                    object? conversationResult = await conversationCommand.ExecuteScalarAsync(cancellationToken);
                    if (conversationResult is not null && conversationResult != DBNull.Value)
                        conversationId = Convert.ToInt32(conversationResult);
                }

                DateTime sentAtUtc = DateTime.UtcNow;

                if (!conversationId.HasValue)
                {
                    await using (var createConversationCommand = new SqlCommand(createConversationSql, connection, transaction))
                    {
                        createConversationCommand.Parameters.Add(new SqlParameter("@SenderUserId", SqlDbType.Int) { Value = senderUserId });
                        createConversationCommand.Parameters.Add(new SqlParameter("@SentAtUtc", SqlDbType.DateTime2) { Value = sentAtUtc });

                        object? createdConversation = await createConversationCommand.ExecuteScalarAsync(cancellationToken);
                        conversationId = Convert.ToInt32(createdConversation);
                    }

                    await using (var senderParticipantCommand = new SqlCommand(addParticipantSql, connection, transaction))
                    {
                        senderParticipantCommand.Parameters.Add(new SqlParameter("@ConversationId", SqlDbType.Int) { Value = conversationId.Value });
                        senderParticipantCommand.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = senderUserId });
                        senderParticipantCommand.Parameters.Add(new SqlParameter("@SentAtUtc", SqlDbType.DateTime2) { Value = sentAtUtc });
                        senderParticipantCommand.Parameters.Add(new SqlParameter("@LastReadAtUtc", SqlDbType.DateTime2) { Value = sentAtUtc });
                        await senderParticipantCommand.ExecuteNonQueryAsync(cancellationToken);
                    }

                    await using (var recipientParticipantCommand = new SqlCommand(addParticipantSql, connection, transaction))
                    {
                        recipientParticipantCommand.Parameters.Add(new SqlParameter("@ConversationId", SqlDbType.Int) { Value = conversationId.Value });
                        recipientParticipantCommand.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = recipientUserId });
                        recipientParticipantCommand.Parameters.Add(new SqlParameter("@SentAtUtc", SqlDbType.DateTime2) { Value = sentAtUtc });
                        recipientParticipantCommand.Parameters.Add(new SqlParameter("@LastReadAtUtc", SqlDbType.DateTime2) { Value = DBNull.Value });
                        await recipientParticipantCommand.ExecuteNonQueryAsync(cancellationToken);
                    }
                }

                await using (var messageCommand = new SqlCommand(insertMessageSql, connection, transaction))
                {
                    messageCommand.Parameters.Add(new SqlParameter("@ConversationId", SqlDbType.Int) { Value = conversationId.Value });
                    messageCommand.Parameters.Add(new SqlParameter("@SenderUserId", SqlDbType.Int) { Value = senderUserId });
                    AddNVarChar(messageCommand, "@Body", 4000, body.Trim());
                    messageCommand.Parameters.Add(new SqlParameter("@SentAtUtc", SqlDbType.DateTime2) { Value = sentAtUtc });

                    await messageCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var updateConversationCommand = new SqlCommand(updateConversationSql, connection, transaction))
                {
                    updateConversationCommand.Parameters.Add(new SqlParameter("@ConversationId", SqlDbType.Int) { Value = conversationId.Value });
                    updateConversationCommand.Parameters.Add(new SqlParameter("@SentAtUtc", SqlDbType.DateTime2) { Value = sentAtUtc });
                    await updateConversationCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var updateSenderReadCommand = new SqlCommand(updateSenderReadSql, connection, transaction))
                {
                    updateSenderReadCommand.Parameters.Add(new SqlParameter("@ConversationId", SqlDbType.Int) { Value = conversationId.Value });
                    updateSenderReadCommand.Parameters.Add(new SqlParameter("@SenderUserId", SqlDbType.Int) { Value = senderUserId });
                    updateSenderReadCommand.Parameters.Add(new SqlParameter("@SentAtUtc", SqlDbType.DateTime2) { Value = sentAtUtc });
                    await updateSenderReadCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return SendDirectMessageResult.Success(conversationId.Value);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static string BuildDisplayName(string login, string firstName, string lastName)
        {
            string fullName = $"{firstName} {lastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? login : fullName;
        }

        private static string Normalize(string value)
            => (value ?? string.Empty).Trim().ToUpperInvariant();

        private static void AddNVarChar(SqlCommand command, string name, int size, string value)
        {
            command.Parameters.Add(
                new SqlParameter(name, SqlDbType.NVarChar, size)
                {
                    Value = value
                });
        }
    }

    public sealed class MessageDialogListItem
    {
        public int ConversationId { get; set; }
        public int OtherUserId { get; set; }
        public string OtherUserLogin { get; set; } = "";
        public string OtherUserName { get; set; } = "";
        public string Preview { get; set; } = "";
        public DateTime LastMessageAtUtc { get; set; }
        public int UnreadCount { get; set; }
    }

    public sealed class DirectMessageItem
    {
        public int Id { get; set; }
        public int SenderUserId { get; set; }
        public string Body { get; set; } = "";
        public DateTime SentAtUtc { get; set; }
    }

    public enum SendDirectMessageStatus
    {
        Success = 0,
        RecipientNotFound = 1,
        CannotMessageSelf = 2
    }

    public sealed class SendDirectMessageResult
    {
        public SendDirectMessageStatus Status { get; private init; }
        public int? ConversationId { get; private init; }

        public static SendDirectMessageResult Success(int conversationId)
            => new()
            {
                Status = SendDirectMessageStatus.Success,
                ConversationId = conversationId
            };

        public static SendDirectMessageResult RecipientNotFound()
            => new()
            {
                Status = SendDirectMessageStatus.RecipientNotFound
            };

        public static SendDirectMessageResult CannotMessageSelf()
            => new()
            {
                Status = SendDirectMessageStatus.CannotMessageSelf
            };
    }
}
