document.addEventListener('DOMContentLoaded', () => {
    window.getCspNonce = function () {
        const nonceSource = document.querySelector('script[nonce], style[nonce], meta[name="csp-nonce"]');
        if (!nonceSource) {
            return '';
        }

        if (typeof nonceSource.nonce === 'string' && nonceSource.nonce.length > 0) {
            return nonceSource.nonce;
        }

        return nonceSource.getAttribute('nonce') || nonceSource.getAttribute('content') || '';
    };

    window.validateAjaxForm = function (form) {
        if (!form) {
            return false;
        }

        if (typeof form.checkValidity === 'function' && !form.checkValidity()) {
            if (typeof form.reportValidity === 'function') {
                form.reportValidity();
            }
            return false;
        }

        if (typeof $ !== 'undefined') {
            const $form = $(form);
            if ($.validator && typeof $form.valid === 'function' && !$form.valid()) {
                return false;
            }
        }

        return true;
    };

    window.getResultMessages = function (result) {
        if (!result) {
            return [];
        }

        const messages = [];

        if (Array.isArray(result.messages)) {
            result.messages
                .filter(message => typeof message === 'string' && message.trim().length > 0)
                .forEach(message => messages.push(message.trim()));
        }

        if (Array.isArray(result.validationErrors)) {
            result.validationErrors
                .map(error => error && typeof error.errorMessage === 'string' ? error.errorMessage.trim() : '')
                .filter(message => message.length > 0)
                .forEach(message => messages.push(message));
        }

        return [...new Set(messages)];
    };

    window.getPrimaryResultMessage = function (result, fallbackMessage) {
        const messages = window.getResultMessages(result);
        return messages.length > 0 ? messages[0] : fallbackMessage;
    };

    window.getAjaxErrorResult = function (xhr) {
        if (!xhr) {
            return null;
        }

        if (xhr.responseJSON && typeof xhr.responseJSON === 'object') {
            return xhr.responseJSON;
        }

        if (typeof xhr.responseText === 'string' && xhr.responseText.trim().length > 0) {
            try {
                return JSON.parse(xhr.responseText);
            } catch {
                return null;
            }
        }

        return null;
    };

    window.getAjaxErrorMessage = function (xhr, fallbackMessage) {
        const result = window.getAjaxErrorResult(xhr);
        if (result) {
            const message = window.getPrimaryResultMessage(result, '');
            if (message) {
                return message;
            }
        }

        if (xhr && xhr.status === 404) {
            return 'We could not find what you requested. Please refresh and try again.';
        }

        if (xhr && xhr.status === 403) {
            return 'You do not have permission to complete this action.';
        }

        if (xhr && xhr.status === 400) {
            return 'Some of the submitted information needs attention. Please review it and try again.';
        }

        return fallbackMessage || 'Something went wrong while processing your request. Please try again.';
    };

    window.showRequestError = function (options) {
        const config = options || {};
        const title = config.title || 'Something went wrong';
        const message = window.getAjaxErrorMessage(config.xhr, config.fallbackMessage);

        if (typeof Swal !== 'undefined') {
            return Swal.fire({
                icon: 'error',
                title: title,
                text: message
            });
        }

        alert(message);
        return Promise.resolve();
    };

    window.injectHtmlWithNonce = function (target, html) {
        if (!target) {
            return;
        }

        const nonce = window.getCspNonce();
        const temp = document.createElement('div');
        temp.innerHTML = html;

        const scripts = Array.from(temp.querySelectorAll('script'));
        scripts.forEach(script => script.remove());

        target.replaceChildren(...Array.from(temp.childNodes));

        scripts.forEach(originalScript => {
            const script = document.createElement('script');

            Array.from(originalScript.attributes).forEach(attribute => {
                script.setAttribute(attribute.name, attribute.value);
            });

            if (nonce) {
                script.nonce = nonce;
                if (!script.getAttribute('nonce')) {
                    script.setAttribute('nonce', nonce);
                }
            }

            script.textContent = originalScript.textContent;
            target.appendChild(script);
        });
    };

    const environmentMeta = document.querySelector('meta[name="app-environment"]');
    const isDevelopmentEnvironment = environmentMeta && environmentMeta.content === 'Development';

    if ('serviceWorker' in navigator) {
        window.addEventListener('load', () => {
            if (isDevelopmentEnvironment) {
                navigator.serviceWorker.getRegistrations()
                    .then(registrations => Promise.all(registrations.map(registration => registration.unregister())))
                    .catch(err => console.log(`Service Worker: Cleanup Error: ${err}`));
                return;
            }

            navigator.serviceWorker.register('/service-worker.js')
                .then(() => console.log('Service Worker: Registered'))
                .catch(err => console.log(`Service Worker: Error: ${err}`));
        });
    }

    const mobileMenuBtn = document.getElementById('mobile-menu-btn');
    const sidebar = document.querySelector('.sidebar');
    const overlay = document.getElementById('sidebar-overlay');
    const layout = document.querySelector('.app-layout');

    // Create overlay if it doesn't exist
    let activeOverlay = document.getElementById('sidebar-overlay');
    if (!activeOverlay && layout && sidebar) {
        activeOverlay = document.createElement('div');
        activeOverlay.className = 'sidebar-overlay';
        activeOverlay.id = 'sidebar-overlay';
        layout.insertBefore(activeOverlay, sidebar);

        activeOverlay.addEventListener('click', () => {
            sidebar.classList.remove('open');
            activeOverlay.classList.remove('open');
        });
    }

    if (mobileMenuBtn && sidebar) {
        mobileMenuBtn.addEventListener('click', (e) => {
            e.stopPropagation(); // prevent immediate closing
            sidebar.classList.toggle('open');
            if (activeOverlay) {
                activeOverlay.classList.toggle('open');
            }
        });
    }

    // Handle responsive grid changes for specific inline styled elements
    const handleResize = () => {
        const isMobile = window.innerWidth <= 768;

        // Find panels wrapping grid areas
        document.querySelectorAll('div[style*="display: grid"]').forEach(grid => {
            if (grid.style.gridTemplateColumns.includes('2fr 1fr') || grid.style.gridTemplateColumns.includes('repeat(3, 1fr)')) {
                if (isMobile) {
                    grid.style.gridTemplateColumns = '1fr';
                } else {
                    // Restore based on original intention (hacky way without knowing exact original)
                    if (grid.children.length === 2 && grid.children[0].className === 'panel') {
                        grid.style.gridTemplateColumns = '2fr 1fr';
                    } else if (grid.children.length === 3) {
                        grid.style.gridTemplateColumns = 'repeat(3, 1fr)';
                    }
                }
            }
        });
    };

    window.addEventListener('resize', handleResize);
    handleResize(); // trigger on load

    // Close sidebar on click outside
    document.addEventListener('click', (e) => {
        // Ensure the click didn't happen inside the sidebar OR on the button itself
        const isClickInsideSidebar = sidebar && sidebar.contains(e.target);
        const isClickOnToggle = mobileMenuBtn && (mobileMenuBtn.contains(e.target) || e.target === mobileMenuBtn);

        if (sidebar && sidebar.classList.contains('open') && !isClickInsideSidebar && !isClickOnToggle) {
            sidebar.classList.remove('open');
            if (activeOverlay) {
                activeOverlay.classList.remove('open');
            }
        }
    });
    if (typeof $ !== 'undefined' && $.fn.DataTable) {

        // Establish GLOBAL default Enterprise styling for any DataTable initialized anywhere in the app
        $.extend(true, $.fn.dataTable.defaults, {
            language: {
                search: "",
                searchPlaceholder: "Search records...",
                lengthMenu: ""
            },
            responsive: true,
            dom: '<"table-toolbar datatables-injected"<"table-filters"l>f>t<"pagination"ip>',
            initComplete: function () {
                $('.dataTables_filter input').addClass('form-input');
                $('.dataTables_length select').addClass('form-select');
            }
        });

        // Initialize generic data tables
        // (Skipping tables that users want to initialize manually using scripts in their views via .manual-init class)
        $('.data-table:not(.manual-init)').each(function () {
            // Special rules for dashboard compact tables
            const $table = $(this);
            const isDashboard = $table.closest('.grid-stats').length > 0 || $table.closest('.panel').parent().css('display') === 'grid';

            // Find the closest custom toolbar that we might want to connect to
            const $panel = $table.closest('.panel');
            const $customSearch = $panel.find('.table-toolbar .form-input[placeholder*="Filter"], .table-toolbar .form-input[placeholder*="Search"]');
            const $customFilters = $panel.find('.table-toolbar .form-select');

            // Set up DataTable configuration
            const dtConfig = {
                paging: !isDashboard,
                searching: !isDashboard,
                info: !isDashboard,
            };

            // Override global dom for tiny dashboard tables
            if (isDashboard) {
                dtConfig.dom = 't';
            }

            // Only enable server-side processing for main tables (not dashboard tiny tables)
            // You can also drive this off a data attribute on the table like data-server-side="true"
            if (!isDashboard) {
                // Enable DataTables server-side mode
                dtConfig.serverSide = true;
                dtConfig.processing = true;

                // Example AJAX block for ASP.NET Core MVC
                // Replace the URL with your actual endpoint e.g., '/Applications/GetGridData'
                dtConfig.ajax = {
                    url: $table.data('url') || '/api/placeholder-endpoint',
                    type: 'POST',
                    data: function (d) {
                        // Append our custom dropdown filters to the server payload automatically
                        $customFilters.each(function (index, el) {
                            d['custom_filter_' + index] = $(el).val();
                        });

                        // Append custom search input if present
                        if ($customSearch.length > 0) {
                            // DataTables sends its own search parameter natively, 
                            // but you can append custom ones here explicitly too if desired
                            d.customSearchValue = $customSearch.val();
                        }
                    },
                    // MOCK ERROR HANDLER (So the UI doesn't visually break when hitting the dummy endpoint locally)
                    // Remove this error block when connecting to your real backend!
                    error: function (xhr, error, thrown) {
                        console.warn("DataTable AJAX failed. Waiting for real ASP.NET Core endpoint to be wired up.");
                    }
                };
            }

            const dt = $table.DataTable(dtConfig);

            // Wire up custom search bar
            if ($customSearch.length > 0) {
                // Note: for server-side search it's often better to debounce the keyup event
                let searchTimeout;
                $customSearch.on('keyup', function () {
                    clearTimeout(searchTimeout);
                    const val = this.value;
                    searchTimeout = setTimeout(() => {
                        dt.search(val).draw();
                    }, 400); // 400ms delay to prevent spamming
                });
            }

            // Wire up custom dropdown filters
            if ($customFilters.length > 0) {
                $customFilters.on('change', function () {
                    dt.draw();
                });
            }
        });

        // Hide the hardcoded dummy paginations if DataTables takes over
        $('.pagination:not(.dataTables_paginate)').hide();
        // Hide the dummy toolbar ONLY if it doesn't contain custom filters we want to keep
        $('.datatables-injected').hide();
    }

    // Modal System
    const appModal = document.getElementById('appModal');
    const appModalDialog = appModal ? appModal.querySelector('.modal-dialog') : null;
    const closeModalBtn = document.getElementById('closeModal');

    window.closeModal = function () {
        if (appModal) {
            appModal.classList.remove('open');
            setTimeout(() => {
                $('#modalContent').empty();
            }, 300);
        }
    };

    window.openModal = function (config) {
        if (!appModal) return;

        const { title, subtitle, icon, url, onSuccess } = config;

        document.getElementById('modalTitle').innerText = title || '';
        document.getElementById('modalSubtitle').innerText = subtitle || '';
        if (icon) {
            document.getElementById('modalIcon').innerHTML = icon;
        }

        // Use .off().on() pattern or ensure clean slate
        window.onModalSuccess = onSuccess;

        $.ajax({
            url: url,
            type: 'GET',
            cache: false,
            success: function (html) {
                const modalContent = document.getElementById('modalContent');
                window.injectHtmlWithNonce(modalContent, html);

                if (typeof $ !== 'undefined' && $.validator && $.validator.unobtrusive) {
                    $.validator.unobtrusive.parse($('#modalContent'));
                }

                appModal.classList.add('open');
                $('#modalContent .btn-secondary, #modalContent #cancelCreateTenant').off('click').on('click', window.closeModal);
            },
            error: function (xhr) {
                window.showRequestError({
                    title: 'Unable to Open Form',
                    xhr: xhr,
                    fallbackMessage: 'We could not load this form right now. Please try again.'
                });
            }
        });
    };

    if (closeModalBtn) {
        closeModalBtn.addEventListener('click', window.closeModal);
    }

    // User Menu Dropdown Toggle
    const userMenuToggle = document.getElementById('userMenuToggle');
    const userDropdown = document.getElementById('userDropdown');

    if (userMenuToggle && userDropdown) {
        userMenuToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            userDropdown.classList.toggle('open');
            if (tenantDropdown) tenantDropdown.classList.remove('open');
        });
    }

    // Tenant Switcher Toggle
    const tenantMenuToggle = document.getElementById('tenantMenuToggle');
    const tenantDropdown = document.getElementById('tenantDropdown');

    if (tenantMenuToggle && tenantDropdown) {
        tenantMenuToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            tenantDropdown.classList.toggle('open');
            if (userDropdown) userDropdown.classList.remove('open');
        });
    }

    // Close all dropdowns when clicking elsewhere
    document.addEventListener('click', (e) => {
        if (userDropdown && userDropdown.classList.contains('open') && !userDropdown.contains(e.target)) {
            userDropdown.classList.remove('open');
        }
        if (tenantDropdown && tenantDropdown.classList.contains('open') && !tenantDropdown.contains(e.target)) {
            tenantDropdown.classList.remove('open');
        }
    });

    // Close on click outside modal dialog
    if (appModal) {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && appModal.classList.contains('open')) {
                window.closeModal();
            }
        });
    }
});
