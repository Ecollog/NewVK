(function () {
    function setRegisterFieldsDisabled(isDisabled) {
        const regFields = document.querySelectorAll('#reg-extra input, #reg-extra select, #reg-extra textarea');
        regFields.forEach(field => {
            field.disabled = isDisabled;
        });
    }

    function setSubmitText(isRegister) {
        const submitBtn = document.getElementById('submit-btn');
        if (submitBtn) {
            submitBtn.textContent = isRegister ? 'Зарегистрироваться' : 'Войти';
        }
    }

    function setActive(tab) {
        const loginTab = document.getElementById('tab-login');
        const regTab = document.getElementById('tab-register');
        const mode = document.getElementById('mode');
        const regExtra = document.getElementById('reg-extra');
        const rememberRow = document.getElementById('remember-row');

        if (!loginTab || !regTab || !mode || !regExtra) return;

        const isRegister = tab === 'register';

        mode.value = isRegister ? 'register' : 'login';

        regTab.classList.toggle('active', isRegister);
        loginTab.classList.toggle('active', !isRegister);
        regExtra.classList.toggle('open', isRegister);

        if (rememberRow) {
            rememberRow.classList.toggle('hidden-row', isRegister);
        }

        setSubmitText(isRegister);
        setRegisterFieldsDisabled(!isRegister);
    }

    function getCurrentMode() {
        const mode = document.getElementById('mode');
        return mode?.value === 'register' ? 'register' : 'login';
    }

    window.switchMode = function (tab) {
        setActive(tab);
    };

    document.addEventListener('DOMContentLoaded', () => {
        const form = document.getElementById('auth-form');
        setActive(getCurrentMode());

        if (form) {
            form.addEventListener('submit', () => {
                setActive(getCurrentMode());
            });
        }
    });
})();