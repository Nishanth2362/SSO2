/**
 * SSO Premium Login Step transitions and client-side logic
 * Supports Mobile OTP (2 steps) and Credentials (3 steps)
 */
document.addEventListener('DOMContentLoaded', () => {
    const stepInput = document.getElementById('currentStep');
    const flowInput = document.getElementById('loginFlowInput');
    const backButton = document.getElementById('loginBackButton');
    const loginContinueText = document.getElementById('loginContinueText');
    const loginForm = document.getElementById('loginForm');
    const loginAlertBox = document.getElementById('loginAlertBox');

    // Method switcher
    const methodBtns = document.querySelectorAll('.auth-method-btn');

    // Groups
    const grpCredId = document.getElementById('group-credentials-identifier');
    const grpMobId = document.getElementById('group-mobile-identifier');
    const grpCredPwd = document.getElementById('group-credentials-password');
    const grpOtp = document.getElementById('group-otp');

    // Inputs
    const userNameInput = document.getElementById('UserName');
    const phoneEmailInput = document.getElementById('PhoneOrEmail');
    const passwordInput = document.getElementById('Password');
    const otpInput = document.getElementById('TwoFactorCode');
    const previewUserName = document.getElementById('previewUserName');
    const avatarLetter = document.getElementById('avatarLetter');
    const changeAccountButton = document.getElementById('changeAccountButton');
    const resendOtpBtn = document.getElementById('resendOtpBtn');
    let resendTimer = null;

    // Helper to display short, user-friendly error message
    const showLoginError = (input, message) => {
        const alertBox = document.getElementById('loginAlertBox');
        const alertMsg = document.getElementById('loginAlertMessage');
        if (alertBox) {
            alertBox.className = 'chalk-alert chalk-alert-error';
            if (alertMsg) alertMsg.textContent = message;
            else alertBox.textContent = message;
            alertBox.style.display = 'block';
        }

        if (input) {
            const formGroup = input.closest('.form-group');
            if (formGroup) {
                const errSpan = formGroup.querySelector('.text-danger');
                if (errSpan) {
                    errSpan.textContent = message;
                }
            }
            input.focus();
            input.classList.add('input-validation-error');
        }
    };

    const clearLoginErrors = () => {
        const alertBox = document.getElementById('loginAlertBox');
        if (alertBox) alertBox.style.display = 'none';
        document.querySelectorAll('.form-group .text-danger').forEach(el => el.textContent = '');
        document.querySelectorAll('.form-input').forEach(el => el.classList.remove('input-validation-error'));
    };

    // UI Updates
    const setStep = (step, flow) => {
        clearLoginErrors();
        if (step === 1) {
            if (typeof resendTimer !== 'undefined' && resendTimer) clearInterval(resendTimer);
            if (resendOtpBtn) {
                resendOtpBtn.textContent = 'Resend Code';
                resendOtpBtn.classList.remove('disabled');
            }
        }
        if (stepInput) stepInput.value = step;
        if (flowInput) flowInput.value = flow;

        const isMobile = flow === 'mobile';

        // Hide all groups
        if (grpCredId) grpCredId.style.display = 'none';
        if (grpMobId) grpMobId.style.display = 'none';
        if (grpCredPwd) grpCredPwd.style.display = 'none';
        if (grpOtp) grpOtp.style.display = 'none';

        // Method switcher
        const methodSwitcher = document.getElementById('methodSwitcher');
        if (methodSwitcher) methodSwitcher.style.display = step === 1 ? 'flex' : 'none';

        // Back Button
        if (backButton) backButton.style.display = step > 1 ? 'flex' : 'none';

        // Alerts
        if (step === 1 && loginAlertBox) loginAlertBox.style.display = 'none';

        // Setup Header and button text
        const title = document.getElementById('loginCardTitle');
        if (title) {
            if (step === 1) title.textContent = "Sign In";
            else if (step === 2 && !isMobile) title.textContent = "Welcome Back";
            else title.textContent = "Verification";
        }

        if (loginContinueText) {
            loginContinueText.textContent = step > 1 ? "Sign In" : "Continue";
        }

        // User Preview
        const preview = document.getElementById('userPreview');
        if (preview) preview.style.display = step > 1 ? 'block' : 'none';
        if (step > 1) {
            const ident = isMobile ? phoneEmailInput?.value : userNameInput?.value;
            if (previewUserName) previewUserName.textContent = ident;
            if (avatarLetter && ident) avatarLetter.textContent = ident.charAt(0).toUpperCase();
        }

        // Setup Progress Indicator
        const s1 = document.getElementById('stepNode1');
        const l1 = document.getElementById('stepLine1');
        const s2 = document.getElementById('stepNode2');
        const s2Label = document.getElementById('step2Label');

        if (s1) s1.className = `chalk-step-node ${step > 1 ? 'chalk-step-completed' : 'chalk-step-active'}`;
        if (l1) l1.className = `chalk-step-line ${step > 1 ? 'chalk-step-active' : ''}`;

        if (s2Label) s2Label.textContent = isMobile ? 'OTP' : 'Password';

        if (s2) {
            if (step === 2) {
                s2.className = 'chalk-step-node chalk-step-active';
            } else if (step > 2) {
                s2.className = 'chalk-step-node chalk-step-completed';
            } else {
                s2.className = 'chalk-step-node';
            }
        }

        // Show correct inputs
        if (step === 1) {
            if (isMobile) {
                if (grpMobId) {
                    grpMobId.style.display = 'block';
                    if (phoneEmailInput) updateClearBtnVisibility(phoneEmailInput);
                    setTimeout(() => phoneEmailInput?.focus(), 100);
                }
            } else {
                if (grpCredId) {
                    grpCredId.style.display = 'block';
                    if (userNameInput) updateClearBtnVisibility(userNameInput);
                    setTimeout(() => userNameInput?.focus(), 100);
                }
            }
        }
        else if (step === 2) {
            if (isMobile) {
                if (grpOtp) {
                    grpOtp.style.display = 'block';
                    setTimeout(() => otpInput?.focus(), 100);
                }
            } else {
                if (grpCredPwd) {
                    grpCredPwd.style.display = 'block';
                    setTimeout(() => passwordInput?.focus(), 100);
                }
            }
        }
        else if (step === 3 && !isMobile) {
            if (grpOtp) {
                grpOtp.style.display = 'block';
                setTimeout(() => otpInput?.focus(), 100);
            }
        }
    };

    // Binding Switcher
    methodBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            if (btn.classList.contains('active')) return;
            methodBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            const newFlow = btn.getAttribute('data-flow');

            // Clear validation errors
            if ($(loginForm).data('validator')) {
                $(loginForm).validate().resetForm();
            }
            if (loginAlertBox) loginAlertBox.style.display = 'none';

            setStep(1, newFlow);
        });
    });

    // Back / Edit
    const goBack = (e) => {
        if (e) {
            if (typeof e.preventDefault === 'function') e.preventDefault();
            if (typeof e.stopPropagation === 'function') e.stopPropagation();
        }
        const flow = flowInput?.value || 'credentials';
        const current = parseInt(stepInput?.value || "1");

        // Clear password field and form validation errors when stepping back
        if (passwordInput) passwordInput.value = '';
        clearLoginErrors();
        if (typeof $ !== 'undefined' && $(loginForm).length && $(loginForm).data('validator')) {
            $(loginForm).validate().resetForm();
        }

        if (current === 3) setStep(2, flow); // 2FA -> Password
        else setStep(1, flow); // Password/OTP -> Ident
    };

    window.goBackToStep1 = goBack;

    if (backButton) backButton.addEventListener('click', goBack);
    if (changeAccountButton) changeAccountButton.addEventListener('click', goBack);

    // Event delegation fallback
    if (typeof $ !== 'undefined') {
        $(document).on('click', '#loginBackButton, #changeAccountButton, .chalk-edit-identity-btn', function (e) {
            goBack(e);
        });
    }

    // Resend OTP with live countdown timer and alert messaging
    if (resendOtpBtn) {
        resendOtpBtn.addEventListener('click', (e) => {
            e.preventDefault();
            if (resendOtpBtn.classList.contains('disabled')) return;

            clearLoginErrors();
            resendOtpBtn.classList.add('disabled');
            const originalText = 'Resend Code';
            resendOtpBtn.textContent = 'Sending...';

            const flow = flowInput?.value || 'credentials';
            const ident = flow === 'mobile' ? phoneEmailInput?.value : userNameInput?.value;
            const returnUrl = document.querySelector('input[name="ReturnUrl"]')?.value || '';
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

            if (!ident) {
                showLoginError(null, "Please enter your username or email first.");
                resendOtpBtn.classList.remove('disabled');
                resendOtpBtn.textContent = originalText;
                return;
            }

            const fd = new FormData();
            fd.append('identifier', ident.trim());
            fd.append('flow', flow);
            fd.append('returnUrl', returnUrl);
            fd.append('__RequestVerificationToken', token);

            fetch('/Login/ResendOtp', { method: 'POST', body: fd })
                .then(r => r.json())
                .then(data => {
                    if (data && data.success) {
                        const infoBox = document.getElementById('infoAlertBox');
                        if (infoBox) {
                            infoBox.textContent = data.message || "A new verification code has been sent.";
                            infoBox.style.display = 'block';
                        } else {
                            const alertBox = document.getElementById('loginAlertBox');
                            const alertMsg = document.getElementById('loginAlertMessage');
                            if (alertBox) {
                                alertBox.className = 'chalk-alert chalk-alert-info';
                                if (alertMsg) alertMsg.textContent = data.message || "A new verification code has been sent.";
                                alertBox.style.display = 'block';
                            }
                        }

                        // Live countdown timer (60 seconds)
                        let countdown = 60;
                        resendOtpBtn.textContent = `Resend in ${countdown}s`;
                        clearInterval(resendTimer);
                        resendTimer = setInterval(() => {
                            countdown--;
                            if (countdown > 0) {
                                resendOtpBtn.textContent = `Resend in ${countdown}s`;
                            } else {
                                clearInterval(resendTimer);
                                resendOtpBtn.textContent = originalText;
                                resendOtpBtn.classList.remove('disabled');
                            }
                        }, 1000);
                    } else {
                        showLoginError(null, data?.message || "Failed to resend code. Please try again.");
                        resendOtpBtn.textContent = originalText;
                        resendOtpBtn.classList.remove('disabled');
                    }
                })
                .catch(() => {
                    showLoginError(null, "Network error. Failed to resend code.");
                    resendOtpBtn.textContent = originalText;
                    resendOtpBtn.classList.remove('disabled');
                });
        });
    }

    // Form Submit Handler with strict client-side validation
    if (loginForm) {
        $(loginForm).on('submit', function (e) {
            clearLoginErrors();
            const step = parseInt(stepInput?.value || "1");
            const flow = flowInput?.value || 'credentials';

            // ── STEP 1: Credentials Flow ──
            if (step === 1 && flow === 'credentials') {
                const val = userNameInput?.value ? userNameInput.value.trim() : '';
                if (!val) {
                    e.preventDefault();
                    showLoginError(userNameInput, "Please enter your email or username.");
                    return false;
                }

                e.preventDefault();
                if (loginContinueText) loginContinueText.textContent = 'Verifying...';

                const checkUrl = `/Login/CheckUserExists?identifier=${encodeURIComponent(val)}`;
                fetch(checkUrl)
                    .then(r => r.json())
                    .then(data => {
                        if (loginContinueText) loginContinueText.textContent = 'Continue';
                        if (data && data.exists) {
                            clearLoginErrors();
                            setStep(2, 'credentials');
                        } else {
                            showLoginError(userNameInput, "Account not found.");
                        }
                    })
                    .catch(() => {
                        if (loginContinueText) loginContinueText.textContent = 'Continue';
                        showLoginError(userNameInput, "Unable to verify account. Please try again.");
                    });
                return false;
            }

            // ── STEP 1: Mobile Flow ──
            if (step === 1 && flow === 'mobile') {
                const val = phoneEmailInput?.value ? phoneEmailInput.value.trim() : '';
                if (!val) {
                    e.preventDefault();
                    showLoginError(phoneEmailInput, "Please enter your email or phone number.");
                    return false;
                }
            }

            // ── STEP 2: Credentials Flow (Password) ──
            if (step === 2 && flow === 'credentials') {
                const val = passwordInput?.value ? passwordInput.value.trim() : '';
                if (!val) {
                    e.preventDefault();
                    showLoginError(passwordInput, "Please enter your password.");
                    return false;
                }
            }

            // ── STEP 2 (Mobile Flow) or STEP 3 (Credentials Flow): OTP ──
            if ((step === 2 && flow === 'mobile') || step === 3) {
                const val = otpInput?.value ? otpInput.value.trim() : '';
                if (!val) {
                    e.preventDefault();
                    showLoginError(otpInput, "Please enter the verification code.");
                    return false;
                }
                if (val.length < 6) {
                    e.preventDefault();
                    showLoginError(otpInput, "Please enter a valid 6-digit code.");
                    return false;
                }
            }
        });
    }

    // Event delegation fallback for password toggle
    if (typeof $ !== 'undefined') {
        $(document).on('click', '.password-toggle-btn', function (e) {
            e.preventDefault();
            e.stopPropagation();
            window.togglePasswordVisibility(this);
        });
    }

    // Clear Input Button functionality
    const updateClearBtnVisibility = (input) => {
        if (!input) return;
        const wrap = input.closest('.chalk-input-wrap');
        if (!wrap) return;
        const clearBtn = wrap.querySelector('.clear-input-btn');
        if (!clearBtn) return;

        if (input.value && input.value.trim().length > 0) {
            clearBtn.style.display = 'flex';
        } else {
            clearBtn.style.display = 'none';
        }
    };

    // Attach real-time input listeners for clear icon visibility and auto error clearing
    document.querySelectorAll('.chalk-input-wrap input').forEach(input => {
        ['input', 'keyup', 'change', 'focus', 'paste'].forEach(evtName => {
            input.addEventListener(evtName, () => {
                updateClearBtnVisibility(input);
                const formGroup = input.closest('.form-group');
                if (formGroup) {
                    const errSpan = formGroup.querySelector('.text-danger');
                    if (errSpan) errSpan.textContent = '';
                }
                input.classList.remove('input-validation-error');
            });
        });
    });

    // Clear unwanted browser autofill on initial Step 1 load
    const currentStepVal = document.getElementById('currentStep')?.value;
    const hasErr = document.querySelector('.chalk-alert-error') !== null;
    if ((!currentStepVal || currentStepVal === '1') && !hasErr) {
        const clearAutofilled = () => {
            [userNameInput, phoneEmailInput].forEach(inp => {
                if (inp && document.activeElement !== inp) {
                    inp.value = '';
                    updateClearBtnVisibility(inp);
                }
            });
        };
        clearAutofilled();
        setTimeout(clearAutofilled, 50);
        setTimeout(clearAutofilled, 150);
        setTimeout(clearAutofilled, 400);
    } else {
        [userNameInput, phoneEmailInput].forEach(inp => updateClearBtnVisibility(inp));
    }

    // Event delegation for clear input buttons
    if (typeof $ !== 'undefined') {
        $(document).on('click', '.clear-input-btn', function (e) {
            e.preventDefault();
            e.stopPropagation();
            const wrap = $(this).closest('.chalk-input-wrap');
            const input = wrap.find('input');
            if (input.length) {
                input.val('');
                $(this).css('display', 'none');
                input.focus();

                // Clear field validation error if validator is present
                const form = input.closest('form');
                if (form.length && form.data('validator')) {
                    const validator = form.validate();
                    const name = input.attr('name');
                    if (name && validator) {
                        const errors = {};
                        errors[name] = "";
                        validator.showErrors(errors);
                        validator.resetForm();
                    }
                }

                input.trigger('input').trigger('change');
            }
        });
    } else {
        document.addEventListener('click', (e) => {
            const clearBtn = e.target.closest('.clear-input-btn');
            if (!clearBtn) return;
            e.preventDefault();
            e.stopPropagation();
            const wrap = clearBtn.closest('.chalk-input-wrap');
            if (!wrap) return;
            const input = wrap.querySelector('input');
            if (input) {
                input.value = '';
                clearBtn.style.display = 'none';
                input.focus();
                input.dispatchEvent(new Event('input', { bubbles: true }));
                input.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
    }

    // Card Glow Effect
    const card = document.querySelector('.chalk-card');
    if (card) {
        card.addEventListener('mousemove', (e) => {
            const rect = card.getBoundingClientRect();
            card.style.setProperty('--mouse-x', `${e.clientX - rect.left}px`);
            card.style.setProperty('--mouse-y', `${e.clientY - rect.top}px`);
        });
    }
});

// Global Password Toggle function (Native DOM - avoids jQuery type modification restrictions)
window.togglePasswordVisibility = function (btn) {
    if (!btn) return;
    const wrap = btn.closest('.chalk-input-wrap');
    if (!wrap) return;
    const input = wrap.querySelector('input');
    const eye = btn.querySelector('.eye-icon');
    const eyeOff = btn.querySelector('.eye-off-icon');

    if (!input) return;

    if (input.type === 'password') {
        input.type = 'text';
        if (eye) eye.style.display = 'none';
        if (eyeOff) eyeOff.style.display = 'block';
    } else {
        input.type = 'password';
        if (eye) eye.style.display = 'block';
        if (eyeOff) eyeOff.style.display = 'none';
    }
};
