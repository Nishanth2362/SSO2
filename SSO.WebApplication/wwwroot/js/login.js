document.addEventListener('DOMContentLoaded', () => {
    const stepInput = document.getElementById('Step');
    const userNameInput = document.getElementById('UserName');
    const passwordInput = document.getElementById('Password');
    const continueText = document.getElementById('loginContinueText');
    const backButton = document.getElementById('loginBackButton');
    const passwordFields = document.querySelectorAll('[data-login-step="password"]');

    if (!stepInput || !userNameInput || !passwordInput || !continueText) {
        return;
    }

    const setStep = (step) => {
        const showPassword = step === 2;
        stepInput.value = step;
        passwordFields.forEach((field) => {
            field.style.display = showPassword
                ? (field.classList.contains('chalk-checkbox-row') || field.classList.contains('chalk-links') ? 'flex' : 'block')
                : 'none';
        });
        passwordInput.required = showPassword;
        userNameInput.readOnly = showPassword;
        continueText.textContent = showPassword ? 'Sign In' : 'Continue';

        if (showPassword) {
            passwordInput.focus();
        }
    };

    if (backButton) {
        backButton.addEventListener('click', () => {
            passwordInput.value = '';
            setStep(1);
            userNameInput.focus();
        });
    }

    setStep(Number(stepInput.value) === 2 ? 2 : 1);
});
