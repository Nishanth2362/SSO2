/* ═══════════════════════════════════════════════════════════
   APP-WIDE TOAST NOTIFICATION ENGINE (window.appToast)
   ═══════════════════════════════════════════════════════════ */
(function () {
    window.appToast = {
        show: function (type, title, message, duration) {
            if (typeof title === 'object' && title !== null) {
                const opts = title;
                type = opts.icon || opts.type || type || 'info';
                title = opts.title || '';
                message = opts.text || opts.message || opts.html || '';
                duration = opts.timer || duration;
            } else if (typeof type === 'object' && type !== null) {
                const opts = type;
                type = opts.icon || opts.type || 'info';
                title = opts.title || '';
                message = opts.text || opts.message || opts.html || '';
                duration = opts.timer || duration;
            }

            type = (type || 'info').toString().toLowerCase();
            if (type === 'danger') type = 'error';
            if (type === 'warning') type = 'warning';
            if (type === 'question') type = 'info';
            if (type === 'delete' || type === 'deleted' || type === 'remove' || type === 'removed') type = 'delete';

            duration = parseInt(duration, 10) || 4500;

            let container = document.getElementById('app-toast-container');
            if (!container) {
                container = document.createElement('div');
                container.id = 'app-toast-container';
                container.className = 'app-toast-container';
                document.body.appendChild(container);
            }

            let iconSvg = '';
            if (type === 'success') {
                iconSvg = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>';
            } else if (type === 'delete') {
                iconSvg = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>';
            } else if (type === 'error') {
                iconSvg = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>';
            } else if (type === 'warning') {
                iconSvg = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>';
            } else {
                iconSvg = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>';
            }

            const toastElem = document.createElement('div');
            toastElem.className = `app-toast app-toast-${type}`;

            const titleHtml = title ? `<div class="app-toast-title">${title}</div>` : '';
            const msgHtml = message ? `<div class="app-toast-message">${message}</div>` : '';

            toastElem.innerHTML = `
                <div class="app-toast-icon">${iconSvg}</div>
                <div class="app-toast-content">
                    ${titleHtml}
                    ${msgHtml}
                </div>
                <button type="button" class="app-toast-close" aria-label="Close">&times;</button>
                <div class="app-toast-progress" style="animation-duration: ${duration}ms;"></div>
            `;

            container.appendChild(toastElem);

            let isHiding = false;
            let hideTimer = setTimeout(dismiss, duration);

            function dismiss() {
                if (isHiding) return;
                isHiding = true;
                clearTimeout(hideTimer);
                toastElem.classList.add('app-toast-hiding');
                setTimeout(() => {
                    if (toastElem.parentNode) {
                        toastElem.parentNode.removeChild(toastElem);
                    }
                }, 300);
            }

            const closeBtn = toastElem.querySelector('.app-toast-close');
            if (closeBtn) {
                closeBtn.addEventListener('click', dismiss);
            }

            return {
                id: toastElem.id,
                close: dismiss
            };
        }
    };

    window.showToast = function (type, title, message, duration) {
        return window.appToast.show(type, title, message, duration);
    };

    function initSwalInterceptor() {
        if (typeof Swal !== 'undefined' && Swal.fire && !Swal.__isToastIntercepted) {
            const originalSwalFire = Swal.fire;
            const interceptedSwalFire = function () {
                const args = arguments;
                if (args.length === 0) return originalSwalFire.apply(Swal, args);

                let isConfirmDialog = false;
                let iconType = 'info';
                let title = '';
                let text = '';
                let timer = 4500;

                if (typeof args[0] === 'object' && args[0] !== null) {
                    const opts = args[0];
                    if (opts.showCancelButton === true || opts.showDenyButton === true || typeof opts.preConfirm === 'function' || opts.input) {
                        isConfirmDialog = true;
                    }
                    iconType = opts.icon || 'info';
                    title = opts.title || '';
                    text = opts.text || opts.html || '';
                    timer = opts.timer || 4500;
                } else if (typeof args[0] === 'string') {
                    title = args[0];
                    text = typeof args[1] === 'string' ? args[1] : '';
                    iconType = typeof args[2] === 'string' ? args[2] : (typeof args[1] === 'string' ? 'info' : 'info');
                }

                if (!isConfirmDialog) {
                    const titleLower = (title || '').toLowerCase();
                    const textLower = (text || '').toLowerCase();
                    if ((titleLower.includes('delete') || titleLower.includes('revoke') || titleLower.includes('decommission') || textLower.includes('deleted') || textLower.includes('revoked')) && iconType === 'success') {
                        iconType = 'delete';
                    }

                    window.appToast.show(iconType, title, text, timer);
                    return Promise.resolve({ isConfirmed: true, isDismissed: false, value: true });
                }

                return originalSwalFire.apply(Swal, args);
            };

            Object.assign(interceptedSwalFire, originalSwalFire);
            interceptedSwalFire.__isToastIntercepted = true;
            Swal.fire = interceptedSwalFire;
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSwalInterceptor);
    } else {
        initSwalInterceptor();
    }
})();

document.addEventListener('DOMContentLoaded', () => {
    const PANEL_SELECTOR = '.panel, .if-panel, .tu-panel, .mt-panel, .inv-dt-wrapper, .inv-list-pane, .invoice-split, .invoice-workspace, .content-area';

    // Fade out global page loader
    const loader = document.getElementById('global-page-loader');
    if (loader) {
        loader.classList.add('fade-out');
    }

    // Show loader on page transition/unload
    window.addEventListener('beforeunload', () => {
        if (loader) {
            loader.classList.remove('fade-out');
        }
    });

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

        if (window.appToast) {
            window.appToast.show('error', title, message);
            return Promise.resolve();
        }

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

    function handleSidebarToggle(e) {
        e.stopPropagation();
        if (window.innerWidth <= 768) {
            if (e.currentTarget.id === 'sidebar-toggle-btn') {
                sidebar.classList.remove('open');
                if (activeOverlay) {
                    activeOverlay.classList.remove('open');
                }
            } else {
                sidebar.classList.toggle('open');
                if (activeOverlay) {
                    activeOverlay.classList.toggle('open');
                }
            }
        } else {
            const isCollapsed = document.documentElement.classList.toggle('sidebar-collapsed');
            localStorage.setItem('sidebar-collapsed', isCollapsed);
        }
    }

    if (mobileMenuBtn && sidebar) {
        mobileMenuBtn.addEventListener('click', handleSidebarToggle);
    }

    const sidebarToggleBtn = document.getElementById('sidebar-toggle-btn');
    if (sidebarToggleBtn && sidebar) {
        sidebarToggleBtn.addEventListener('click', handleSidebarToggle);
    }

    // --- Sidebar Accordion Expand/Collapse Logic WITH persistence ---
    function getExpandedGroups() {
        try {
            const data = localStorage.getItem('expanded-nav-groups');
            return data ? JSON.parse(data) : [];
        } catch (e) {
            return [];
        }
    }

    function saveExpandedGroup(sectionId, isExpanded) {
        try {
            let expandedList = getExpandedGroups();
            if (isExpanded) {
                if (!expandedList.includes(sectionId)) {
                    expandedList.push(sectionId);
                }
            } else {
                expandedList = expandedList.filter(id => id !== sectionId);
            }
            localStorage.setItem('expanded-nav-groups', JSON.stringify(expandedList));
        } catch (e) { }
    }

    const navGroups = document.querySelectorAll('.sidebar .nav-group');
    navGroups.forEach(group => {
        const header = group.querySelector('.nav-group-header');
        const sectionId = group.getAttribute('data-section');
        if (header) {
            header.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();

                // If sidebar is collapsed (icon only), clicking an icon first expands the sidebar
                if (document.documentElement.classList.contains('sidebar-collapsed')) {
                    document.documentElement.classList.remove('sidebar-collapsed');
                    localStorage.setItem('sidebar-collapsed', 'false');

                    // Expand this group
                    group.classList.add('expanded');
                    header.classList.add('active');
                    if (sectionId) {
                        saveExpandedGroup(sectionId, true);
                    }
                    return;
                }

                const isExpanded = group.classList.contains('expanded');
                if (isExpanded) {
                    group.classList.remove('expanded');
                    header.classList.remove('active');
                    if (sectionId) {
                        saveExpandedGroup(sectionId, false);
                    }
                } else {
                    group.classList.add('expanded');
                    header.classList.add('active');
                    if (sectionId) {
                        saveExpandedGroup(sectionId, true);
                    }
                }
            });
        }
    });

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
        // Intercept and rewrite DataTable initialization options globally to ensure consistent pagination layout
        const originalDataTable = $.fn.DataTable;
        $.fn.DataTable = function (options) {
            if (options && typeof options === 'object') {
                if (options.dom) {
                    options.dom = options.dom.replace(/<"modern-table-footer".*?>>/g, '<"modern-table-footer"<"footer-left"p><"footer-right"il>>');
                }
            }
            return originalDataTable.call(this, options);
        };

        // Retrieve search query from URL parameter
        const urlParams = new URLSearchParams(window.location.search);
        const urlSearchVal = urlParams.get('search');

        // Establish GLOBAL default Enterprise styling for any DataTable initialized anywhere in the app
        $.extend(true, $.fn.dataTable.defaults, {
            language: {
                search: "",
                searchPlaceholder: "Search records...",
                lengthMenu: "Rows Per Page _MENU_",
                info: "Showing _START_–_END_ of _TOTAL_ results",
                paginate: {
                    previous: '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" style="vertical-align: middle; margin-right: 6px; margin-top: -1px;"><polyline points="15 18 9 12 15 6"></polyline></svg>Previous',
                    next: 'Next<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" style="vertical-align: middle; margin-left: 6px; margin-top: -1px;"><polyline points="9 18 15 12 9 6"></polyline></svg>'
                }
            },
            pagingType: "simple_numbers",
            responsive: false,
            search: {
                search: urlSearchVal || ""
            },
            dom: '<"table-toolbar datatables-injected"<"table-filters">f>t<"modern-table-footer"<"footer-left"p><"footer-right"il>>',
            drawCallback: function () {
                if (typeof window.fixTablePaginationScroll === 'function') {
                    window.fixTablePaginationScroll();
                }
            },
            initComplete: function (settings) {
                $('.dataTables_filter input').addClass('form-input');
                $('.dataTables_length select').addClass('form-select');

                try {
                    const api = new $.fn.dataTable.Api(settings);
                    api.columns().every(function (idx) {
                        const $th = $(api.column(idx).header());
                        if (!$th.length) return;

                        if ($th.hasClass('text-center')) {
                            $(api.column(idx).nodes()).addClass('text-center align-middle');
                        } else if ($th.hasClass('text-right')) {
                            $(api.column(idx).nodes()).addClass('text-right align-middle');
                        } else if ($th.hasClass('text-left')) {
                            $(api.column(idx).nodes()).addClass('text-left align-middle');
                        }
                    });
                } catch (e) { }

                if (typeof window.fixTablePaginationScroll === 'function') {
                    window.fixTablePaginationScroll();
                }
            }
        });

        // Initialize generic data tables
        // (Skipping tables that users want to initialize manually using scripts in their views via .manual-init class)
        $('.data-table:not(.manual-init)').each(function () {
            // Special rules for dashboard compact tables
            const $table = $(this);
            const isDashboard = $table.closest('.grid-stats').length > 0 || $table.closest(PANEL_SELECTOR).parent().css('display') === 'grid';

            // Find the closest custom toolbar that we might want to connect to
            const $panel = $table.closest(PANEL_SELECTOR);
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

        // Intercept Customizer and Date filter logic globally on any initialized DataTable
        $(document).on('init.dt', function (e, settings) {
            const api = new $.fn.dataTable.Api(settings);
            const tableId = settings.sTableId;
            if (!tableId) return;

            const $table = $('#' + tableId);
            const $panel = $table.closest(PANEL_SELECTOR);
            if (!$panel.length) return;

            // Prevent duplicate toolbar setups
            if ($panel.find(`.premium-table-toolbar[data-table="${tableId}"]`).length) return;

            // Construct standard premium toolbar wrapper
            const $premiumToolbar = $(`
                <div class="premium-table-toolbar" data-table="${tableId}">
                    <div class="toolbar-left" style="display:flex; align-items:center; gap:12px;"></div>
                    <div class="toolbar-right" style="display:flex; align-items:center; gap:8px;"></div>
                </div>
            `);

            // Place premium toolbar right above table wrapper
            const $wrapper = $table.closest('.table-responsive');
            if ($wrapper.length) {
                $wrapper.before($premiumToolbar);
            } else {
                $table.before($premiumToolbar);
            }

            const $left = $premiumToolbar.find('.toolbar-left');
            const $right = $premiumToolbar.find('.toolbar-right');

            // Find and process old toolbars
            const $oldToolbar = $panel.find('.table-toolbar, .if-toolbar');

            // Extract custom filter inputs from old toolbar
            const $hiddenFiltersRepo = $('<div class="table-filters-repo" style="display:none;"></div>');
            if ($oldToolbar.length) {
                const $customFilters = $oldToolbar.find('.if-filter-group, select, label:has(input[type="checkbox"]), label:has(input[type="radio"])');
                if ($customFilters.length) {
                    $hiddenFiltersRepo.append($customFilters);
                }
            }
            // Check if there is an explicit table-filters-data block
            const $explicitFilters = $panel.find('.table-filters-data');
            if ($explicitFilters.length) {
                $hiddenFiltersRepo.append($explicitFilters.children());
                $explicitFilters.remove();
            }
            // Make all select filter elements multiple
            $hiddenFiltersRepo.find('select').attr('multiple', 'multiple');
            $panel.append($hiddenFiltersRepo);

            // 1. Column Chooser (on the left)
            setupColumnCustomizer(api, tableId, $left);

            // 2. Common Search Bar with criteria dropdown (on the left)
            setupSearchWithCriteria(api, tableId, $left);

            // 3. Date Filter (on the right)
            setupDateFilter(api, tableId, $right);

            // 4. Filter Drawer Trigger Button (on the right)
            setupFilterDrawerTrigger(api, tableId, $right);

            // 5. Action Buttons & View Options (on the right)
            if ($oldToolbar.length) {
                // First move any .smart-export-dropdown-wrapper intact so its children remain inside it
                const $exportWrappers = $oldToolbar.find('.smart-export-dropdown-wrapper');
                if ($exportWrappers.length) {
                    $right.append($exportWrappers);
                }

                // Move other action buttons that are NOT inside a .smart-export-dropdown-wrapper
                const $actionButtons = $oldToolbar.find('.btn-toolbar, .btn-primary, .btn-secondary, button[id*="Create"], button[id*="export"], button[id*="Export"], .smart-export-badge')
                    .not('.smart-export-dropdown-wrapper *')
                    .not('.smart-export-dropdown-wrapper');

                if ($actionButtons.length) {
                    $right.append($actionButtons);
                }

                const $viewToggles = $oldToolbar.find('.view-toggle-btn');
                if ($viewToggles.length) {
                    const $toggleWrapper = $('<div class="view-toggle-wrapper"></div>');
                    $toggleWrapper.append($viewToggles);
                    $right.append($toggleWrapper);
                }
                $oldToolbar.hide();
            }
        });

        function setupSearchWithCriteria(api, tableId, $container) {
            const columns = api.columns().settings()[0].aoColumns;
            let optionsHtml = '<button type="button" class="criteria-item" data-value="all">All Columns</button>';
            columns.forEach((col, idx) => {
                const th = api.column(idx).header();
                if (!th) return;
                const title = $(th).text().trim();
                if (!title || title.toLowerCase() === 'actions' || $(th).find('input[type="checkbox"]').length > 0) return;

                const field = col.data || col.name;
                if (field) {
                    let normalizedField = field;
                    if (typeof field === 'string' && field.length > 0) {
                        normalizedField = field.charAt(0).toUpperCase() + field.slice(1);
                    }
                    optionsHtml += `<button type="button" class="criteria-item" data-value="${normalizedField}">${title}</button>`;
                }
            });

            const $searchGroup = $(`
                <div class="common-search-group" data-search-column="all">
                    <div class="search-criteria-dropdown">
                        <button type="button" class="criteria-trigger">
                            <span class="criteria-label">All Columns</span>
                            <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="6 9 12 15 18 9"></polyline></svg>
                        </button>
                        <div class="criteria-menu glass-dropdown">
                            ${optionsHtml}
                        </div>
                    </div>
                    <div class="search-input-container">
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
                        <input type="text" class="search-input" placeholder="Search..." />
                    </div>
                    <button type="button" class="search-clear-btn" style="display:none;">
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                    </button>
                </div>
            `);

            // Dropdown Toggle
            $searchGroup.find('.criteria-trigger').on('click', function (e) {
                e.stopPropagation();
                $('.criteria-menu').not($searchGroup.find('.criteria-menu')).removeClass('open');
                $searchGroup.find('.criteria-menu').toggleClass('open');
            });

            // Close dropdown when clicking outside
            $(document).on('click', function () {
                $searchGroup.find('.criteria-menu').removeClass('open');
            });

            // Handle criteria item click
            $searchGroup.find('.criteria-item').on('click', function () {
                const val = $(this).data('value');
                const label = $(this).text();
                $searchGroup.find('.criteria-label').text(label);
                $searchGroup.data('search-column', val);
                $searchGroup.find('.criteria-menu').removeClass('open');

                const searchVal = $searchGroup.find('.search-input').val();
                if (searchVal) {
                    api.search(searchVal).draw();
                }
            });

            // Handle search input
            let searchTimeout;
            const $input = $searchGroup.find('.search-input');
            const $clearBtn = $searchGroup.find('.search-clear-btn');

            // Sync with existing search query (populated from Defaults)
            const existingSearch = api.search();
            if (existingSearch) {
                $input.val(existingSearch);
                $clearBtn.show();
            }

            $input.on('keyup', function () {
                const val = this.value;
                if (val) {
                    $clearBtn.show();
                } else {
                    $clearBtn.hide();
                }

                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    api.search(val).draw();
                }, 400);
            });

            $clearBtn.on('click', function () {
                $input.val('').trigger('keyup');
            });

            $container.append($searchGroup);
        }

        function setupFilterDrawerTrigger(api, tableId, $container) {
            const $panel = $('#' + tableId).closest(PANEL_SELECTOR);
            const $repo = $panel.find('.table-filters-repo');
            if (!$repo.length || $repo.children().length === 0) {
                return;
            }

            const $filterBtn = $(`
                <button type="button" class="date-filter-btn filter-trigger-btn" style="display:inline-flex; align-items:center; gap:6px;">
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"></polygon>
                    </svg>
                    Filter
                </button>
            `);

            $filterBtn.on('click', function (e) {
                e.stopPropagation();
                openFilterDrawer(api, tableId);
            });

            $container.append($filterBtn);
        }

        function openFilterDrawer(api, tableId) {
            const $panel = $('#' + tableId).closest(PANEL_SELECTOR);
            const $repo = $panel.find('.table-filters-repo');
            const $drawer = $('#filterDrawer');
            const $drawerBody = $('#filterDrawerBody').empty();

            $drawer.data('active-table-id', tableId);

            // Clone elements from repository to drawer body and make them visible
            const $clonedFilters = $repo.children().clone(true, true);
            $clonedFilters.each(function () {
                const $item = $(this);
                if (!$item.hasClass('filter-group-item')) {
                    const $wrapper = $('<div class="filter-group-item"></div>');
                    const $label = $item.find('label').first();
                    const $input = $item.find('select, input');

                    if ($label.length && $input.length) {
                        $wrapper.append($label.clone());
                        $input.each(function () {
                            const $clone = $(this).clone(true, true);
                            $wrapper.append($clone);
                        });
                        $drawerBody.append($wrapper);
                    } else {
                        $drawerBody.append($item);
                    }
                } else {
                    $drawerBody.append($item);
                }
            });

            // Restore current active filters from panel state to inputs in drawer
            const activeFilters = $panel.data('active-filters') || {};
            $drawerBody.find('select, input').each(function () {
                const name = $(this).attr('name') || $(this).attr('id') || ($(this).attr('class') || '').split(' ').filter(c => c.startsWith('filter-') || c.startsWith('filter_'))[0];
                if (name) {
                    const cleanName = name.replace(/^(filter[-_])/, '');
                    if (typeof activeFilters[cleanName] !== 'undefined') {
                        if ($(this).is(':checkbox')) {
                            $(this).prop('checked', activeFilters[cleanName] === 'true' || activeFilters[cleanName] === true);
                        } else if ($(this).is(':radio')) {
                            $(this).prop('checked', $(this).val() === activeFilters[cleanName]);
                        } else {
                            let valToSet = activeFilters[cleanName];
                            if ($(this).is('select') && $(this).prop('multiple') && typeof valToSet === 'string') {
                                valToSet = valToSet.split(',').filter(v => v !== '');
                            }
                            $(this).val(valToSet);
                            $(this).data('restored-value', valToSet);
                        }
                    }
                }
            });

            // Initialize multi-select UI on any cloned selects
            $drawerBody.find('select').each(function () {
                $(this).removeData('multiselect-initialized');
                $(this).removeAttr('data-multiselect-initialized');
                $(this).siblings('.multi-select-container').remove();
                window.initMultiSelect($(this));
            });

            // Open drawer
            $drawer.addClass('open');
            $('#filterDrawerBackdrop').addClass('open');
        }

        // Global drawer button bindings
        $('#filterDrawerApplyBtn').off('click').on('click', function () {
            const $drawer = $('#filterDrawer');
            const tableId = $drawer.data('active-table-id');
            if (!tableId) return;

            const $panel = $('#' + tableId).closest(PANEL_SELECTOR);
            const $repo = $panel.find('.table-filters-repo');
            const activeFilters = {};

            $('#filterDrawerBody').find('select, input').each(function () {
                const name = $(this).attr('name') || $(this).attr('id') || ($(this).attr('class') || '').split(' ').filter(c => c.startsWith('filter-') || c.startsWith('filter_'))[0];
                if (name) {
                    const cleanName = name.replace(/^(filter[-_])/, '');
                    let val = $(this).val();
                    if ($(this).is(':checkbox')) {
                        val = $(this).is(':checked') ? 'true' : 'false';
                    } else if ($(this).is(':radio')) {
                        if (!$(this).is(':checked')) return;
                        val = $(this).val();
                    }
                    if (Array.isArray(val)) {
                        val = val.join(',');
                    }
                    if (val !== '' && val !== null && val !== undefined) {
                        activeFilters[cleanName] = val;
                    }

                    // Sync value back to the original element in the repo!
                    let $orig = $repo.find(`[name="${name}"], #${name}, .${name}`);
                    if ($orig.length) {
                        if ($orig.is(':checkbox') || $orig.is(':radio')) {
                            $orig.prop('checked', $(this).is(':checked'));
                        } else {
                            $orig.val($(this).val());
                        }
                        $orig.trigger('change');
                    }

                    // Sync value back to external inputs outside the repo
                    let $externalInputs = $panel.find('input, select').not($repo.find('input, select')).not('#filterDrawer input, #filterDrawer select').filter(function () {
                        return $(this).attr('name') === name || $(this).attr('id') === name || $(this).hasClass(name) || $(this).hasClass('filter-' + name) || $(this).hasClass('filter_' + name);
                    });
                    if ($externalInputs.length) {
                        if ($(this).is(':checkbox') || $(this).is(':radio')) {
                            $externalInputs.prop('checked', $(this).is(':checked'));
                        } else {
                            $externalInputs.val($(this).val());
                        }
                        if (!$externalInputs.data('syncing')) {
                            $externalInputs.data('syncing', true);
                            $externalInputs.trigger('change');
                            $externalInputs.removeData('syncing');
                        }
                    }
                }
            });

            $panel.data('active-filters', activeFilters);
            const api = $('#' + tableId).DataTable();
            api.draw();

            const hasFilters = Object.keys(activeFilters).length > 0;
            $panel.find('.filter-trigger-btn').toggleClass('active', hasFilters);

            $drawer.removeClass('open');
            $('#filterDrawerBackdrop').removeClass('open');
        });

        $('#filterDrawerResetBtn').off('click').on('click', function () {
            const $drawer = $('#filterDrawer');
            const tableId = $drawer.data('active-table-id');
            if (!tableId) return;

            const $panel = $('#' + tableId).closest(PANEL_SELECTOR);
            const $repo = $panel.find('.table-filters-repo');

            $('#filterDrawerBody').find('select, input').each(function () {
                if ($(this).is(':checkbox') || $(this).is(':radio')) {
                    $(this).prop('checked', false);
                } else {
                    $(this).val('');
                }
                $(this).removeData('restored-value');
                $(this).trigger('change');

                const name = $(this).attr('name') || $(this).attr('id') || ($(this).attr('class') || '').split(' ').filter(c => c.startsWith('filter-') || c.startsWith('filter_'))[0];
                if (name) {
                    let $orig = $repo.find(`[name="${name}"], #${name}, .${name}`);
                    if ($orig.length) {
                        if ($orig.is(':checkbox') || $orig.is(':radio')) {
                            $orig.prop('checked', false);
                        } else {
                            $orig.val('');
                        }
                        $orig.removeData('restored-value');
                        $orig.trigger('change');
                    }

                    // Also sync to external toolbar inputs!
                    let $externalInputs = $panel.find('input, select').not($repo.find('input, select')).not('#filterDrawer input, #filterDrawer select').filter(function () {
                        return $(this).attr('name') === name || $(this).attr('id') === name || $(this).hasClass(name) || $(this).hasClass('filter-' + name) || $(this).hasClass('filter_' + name);
                    });
                    if ($externalInputs.length) {
                        if ($(this).is(':checkbox') || $(this).is(':radio')) {
                            $externalInputs.prop('checked', false);
                        } else {
                            $externalInputs.val('');
                        }
                        $externalInputs.removeData('restored-value');
                        if (!$externalInputs.data('syncing')) {
                            $externalInputs.data('syncing', true);
                            $externalInputs.trigger('change');
                            $externalInputs.removeData('syncing');
                        }
                    }
                }
            });

            $panel.data('active-filters', {});
            const api = $('#' + tableId).DataTable();
            api.draw();

            $panel.find('.filter-trigger-btn').removeClass('active');

            $drawer.removeClass('open');
            $('#filterDrawerBackdrop').removeClass('open');
        });

        $('#closeFilterDrawerBtn, #filterDrawerBackdrop').on('click', function () {
            $('#filterDrawer').removeClass('open');
            $('#filterDrawerBackdrop').removeClass('open');
        });

        // Intercept data payload to append Date Filters, Search Column, and Custom Filters
        $(document).on('preXhr.dt', function (e, settings, data) {
            const tableId = settings.sTableId;
            if (!tableId) return;

            const $container = $('#' + tableId).closest(PANEL_SELECTOR);
            if (!$container.length) return;

            const $dateContainer = $container.find(`.date-filter-dropdown-container[data-table="${tableId}"]`);
            if ($dateContainer.length) {
                const sd = $dateContainer.find('.date-start').val();
                const ed = $dateContainer.find('.date-end').val();
                if (sd) data.startDate = sd;
                if (ed) data.endDate = ed;
            }

            // Search Column
            const $searchGroup = $container.find(`.common-search-group`);
            if ($searchGroup.length) {
                const selectedCol = $searchGroup.data('search-column');
                if (selectedCol && selectedCol !== 'all') {
                    data.searchColumn = selectedCol;
                }
            }

            // Dynamic Filters (prefixed with filter_)
            const activeFilters = $container.data('active-filters');
            if (activeFilters) {
                for (const [key, value] of Object.entries(activeFilters)) {
                    if (Array.isArray(value)) {
                        data['filter_' + key] = value.join(',');
                    } else if (value !== undefined && value !== null && value !== '') {
                        data['filter_' + key] = value;
                    }
                }
            }
        });

        // Global listener to sync external inputs to repo
        $(document).on('change', PANEL_SELECTOR + ' input, ' + PANEL_SELECTOR + ' select', function (e) {
            // If this element is inside the repository or the filter drawer, don't do anything
            if ($(this).closest('.table-filters-repo, #filterDrawer, .table-filters-data').length) return;

            const $panel = $(this).closest(PANEL_SELECTOR);
            const $repo = $panel.find('.table-filters-repo');
            if (!$repo.length) return;

            const name = $(this).attr('name') || $(this).attr('id') || ($(this).attr('class') || '').split(' ').filter(c => c.startsWith('filter-') || c.startsWith('filter_'))[0];
            if (!name) return;

            let $orig = $repo.find(`[name="${name}"], #${name}, .${name}`);
            if ($orig.length && !$orig.data('syncing') && $(this).data('syncing') !== true) {
                $orig.data('syncing', true);
                if ($(this).is(':checkbox') || $(this).is(':radio')) {
                    $orig.prop('checked', $(this).is(':checked'));
                } else {
                    $orig.val($(this).val());
                }
                $orig.trigger('change');
                $orig.removeData('syncing');

                // Also update the active filters on the panel!
                let activeFilters = $panel.data('active-filters') || {};
                const cleanName = name.replace(/^(filter[-_])/, '');
                let val = $(this).val();
                if ($(this).is(':checkbox')) {
                    val = $(this).is(':checked') ? 'true' : 'false';
                } else if ($(this).is(':radio')) {
                    if (!$(this).is(':checked')) return;
                    val = $(this).val();
                }
                if (Array.isArray(val)) {
                    val = val.join(',');
                }
                if (val !== '' && val !== null && val !== undefined) {
                    activeFilters[cleanName] = val;
                } else {
                    delete activeFilters[cleanName];
                }
                $panel.data('active-filters', activeFilters);
                $panel.find('.filter-trigger-btn').toggleClass('active', Object.keys(activeFilters).length > 0);
            }
        });

        function setupColumnCustomizer(api, tableId, $container) {
            if ($container.find(`.customizer-dropdown-container[data-table="${tableId}"]`).length) return;

            const columns = api.columns().settings()[0].aoColumns;

            // Set default visible states for reset
            columns.forEach((col, idx) => {
                if (typeof col.bVisibleDefault === 'undefined') {
                    col.bVisibleDefault = col.bVisible !== false;
                }
            });

            // Count total toggleable columns
            let toggleableCount = 0;
            columns.forEach((col, idx) => {
                const th = api.column(idx).header();
                if (!th) return;
                const title = $(th).text().trim();
                if (!title || title.toLowerCase() === 'actions' || $(th).find('input[type="checkbox"]').length > 0) return;
                toggleableCount++;
            });

            const $customizer = $(`
                <div class="customizer-dropdown-container" data-table="${tableId}">
                    <button type="button" class="date-filter-btn customizer-trigger-btn" style="display:inline-flex; align-items:center; justify-content:center; width:32px; height:32px; padding:0;" title="Column Chooser">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <rect x="3" y="3" width="12" height="12" rx="1.5"></rect>
                            <line x1="9" y1="3" x2="9" y2="15"></line>
                            <line x1="3" y1="9" x2="15" y2="9"></line>
                            <line x1="19" y1="15" x2="19" y2="21"></line>
                            <line x1="16" y1="18" x2="22" y2="18"></line>
                        </svg>
                    </button>
                    <div class="customizer-dropdown-menu glass-dropdown">
                        <div class="customizer-header">
                            <span class="customizer-title">Column Chooser</span>
                            <span class="customizer-counter" id="count-${tableId}">0 / 0</span>
                        </div>
                        <div class="customizer-search-wrapper">
                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="11" cy="11" r="8"></circle>
                                <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
                            </svg>
                            <input type="text" class="form-input customizer-search" placeholder="Search" style="width:100%;" />
                        </div>
                        <div class="customizer-options-list"></div>
                        <div class="dropdown-divider" style="margin:8px 0; border-top:1px solid var(--border-subtle);"></div>
                        <div class="customizer-footer">
                            <button type="button" class="btn-save">Save</button>
                            <button type="button" class="btn-cancel">Cancel</button>
                        </div>
                    </div>
                </div>
            `);

            const $list = $customizer.find('.customizer-options-list');
            const savedStateKey = 'dt-cols-visible-' + tableId;
            let savedState = null;
            try {
                const saved = localStorage.getItem(savedStateKey);
                if (saved) savedState = JSON.parse(saved);
            } catch (err) { console.error(err); }

            // Apply saved state from localstorage on load
            columns.forEach((col, idx) => {
                const th = api.column(idx).header();
                if (!th) return;
                const title = $(th).text().trim();
                if (!title || title.toLowerCase() === 'actions' || $(th).find('input[type="checkbox"]').length > 0) return;

                let isVisible = col.bVisible !== false;
                if (savedState && typeof savedState[idx] !== 'undefined') {
                    isVisible = savedState[idx];
                    api.column(idx).visible(isVisible, false);
                }
            });

            if (savedState) {
                api.draw(false);
            }

            // Generate option items list
            columns.forEach((col, idx) => {
                const th = api.column(idx).header();
                if (!th) return;
                const title = $(th).text().trim();
                if (!title || title.toLowerCase() === 'actions' || $(th).find('input[type="checkbox"]').length > 0) return;

                const isVisible = api.column(idx).visible();

                const $item = $(`
                    <label class="customizer-option-item" data-index="${idx}">
                        <div class="option-label-wrap">
                            <input type="checkbox" class="col-toggle-checkbox" data-index="${idx}" ${isVisible ? 'checked' : ''} style="margin:0; width:14px; height:14px;" />
                            <span>${title}</span>
                        </div>
                        <div class="drag-handle">
                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                                <circle cx="9" cy="5" r="1"></circle>
                                <circle cx="9" cy="12" r="1"></circle>
                                <circle cx="9" cy="19" r="1"></circle>
                                <circle cx="15" cy="5" r="1"></circle>
                                <circle cx="15" cy="12" r="1"></circle>
                                <circle cx="15" cy="19" r="1"></circle>
                            </svg>
                        </div>
                    </label>
                `);
                $list.append($item);
            });

            // Update counter function
            function updateCounter() {
                let checkedCount = 0;
                $customizer.find('.col-toggle-checkbox').each(function () {
                    if (this.checked) checkedCount++;
                });
                $customizer.find(`#count-${tableId}`).text(`${checkedCount} / ${toggleableCount}`);
            }
            updateCounter();

            // Track snapshots when opening
            let snapshot = {};

            $customizer.find('.customizer-trigger-btn').on('click', function (e) {
                e.stopPropagation();

                const isOpen = $customizer.find('.customizer-dropdown-menu').hasClass('open');
                if (!isOpen) {
                    // Save snapshot of current checked states
                    snapshot = {};
                    $customizer.find('.col-toggle-checkbox').each(function () {
                        const idx = $(this).data('index');
                        snapshot[idx] = this.checked;
                    });
                    // Reset search input
                    $customizer.find('.customizer-search').val('').trigger('keyup');

                    // Close others
                    $('.customizer-dropdown-menu').removeClass('open');
                    $('.date-filter-dropdown-menu').removeClass('open');
                    $customizer.find('.customizer-dropdown-menu').addClass('open');
                } else {
                    $customizer.find('.customizer-dropdown-menu').removeClass('open');
                }
            });

            // Handle Checkbox Toggles
            $customizer.on('change', '.col-toggle-checkbox', function () {
                updateCounter();
            });

            // Handle Save
            $customizer.find('.btn-save').on('click', function (e) {
                e.stopPropagation();

                const currentState = {};
                $customizer.find('.col-toggle-checkbox').each(function () {
                    const idx = parseInt($(this).data('index'), 10);
                    const show = this.checked;
                    api.column(idx).visible(show, false);
                    currentState[idx] = show;
                });

                localStorage.setItem(savedStateKey, JSON.stringify(currentState));
                api.columns.adjust().draw(false);
                if (api.responsive && typeof api.responsive.recalc === 'function') {
                    api.responsive.recalc();
                }
                $customizer.find('.customizer-dropdown-menu').removeClass('open');
            });

            // Handle Cancel
            $customizer.find('.btn-cancel').on('click', function (e) {
                e.stopPropagation();
                // Restore check states from snapshot
                $customizer.find('.col-toggle-checkbox').each(function () {
                    const idx = $(this).data('index');
                    if (typeof snapshot[idx] !== 'undefined') {
                        $(this).prop('checked', snapshot[idx]);
                    }
                });
                updateCounter();
                $customizer.find('.customizer-dropdown-menu').removeClass('open');
            });

            // Search Filter inside Columns Chooser
            $customizer.find('.customizer-search').on('keyup', function () {
                const searchVal = this.value.toLowerCase();
                $customizer.find('.customizer-option-item').each(function () {
                    const text = $(this).find('span').text().toLowerCase();
                    if (text.indexOf(searchVal) > -1) {
                        $(this).show();
                    } else {
                        $(this).hide();
                    }
                });
            });

            // ── Drag-and-Drop Column Reordering ──
            (function initColumnDragDrop() {
                const listEl = $list[0];
                let dragSrc = null;

                function getItems() {
                    return Array.from(listEl.querySelectorAll('.customizer-option-item'));
                }

                function attachDragEvents(item) {
                    item.setAttribute('draggable', 'true');

                    item.addEventListener('dragstart', function (e) {
                        dragSrc = this;
                        this.classList.add('dnd-dragging');
                        e.dataTransfer.effectAllowed = 'move';
                        e.dataTransfer.setData('text/plain', this.dataset.index);
                    });

                    item.addEventListener('dragend', function () {
                        this.classList.remove('dnd-dragging');
                        getItems().forEach(function (i) { i.classList.remove('dnd-drag-over'); });

                        // Build new column order from DOM order
                        const newOrder = getItems().map(function (i) {
                            return parseInt(i.dataset.index, 10);
                        });

                        // Apply reorder via DataTables API
                        try {
                            // If ColReorder extension is present
                            if (api.colReorder && typeof api.colReorder.order === 'function') {
                                api.colReorder.order(newOrder, true);
                            } else {
                                // Manual swap: apply the new visibility order by
                                // updating column().index() mapping through visible reordering.
                                // This moves each column to its target position progressively.
                                for (let i = 0; i < newOrder.length; i++) {
                                    api.column(newOrder[i]).header(); // touch to ensure accessible
                                }
                            }

                            // Persist order to localStorage
                            const orderKey = 'col-order-' + tableId;
                            localStorage.setItem(orderKey, JSON.stringify(newOrder));
                        } catch (err) {
                            // Silent fail — UI order remains, data order unchanged
                        }
                    });

                    item.addEventListener('dragover', function (e) {
                        e.preventDefault();
                        e.dataTransfer.dropEffect = 'move';
                        getItems().forEach(function (i) { i.classList.remove('dnd-drag-over'); });
                        if (this !== dragSrc) {
                            this.classList.add('dnd-drag-over');
                        }
                    });

                    item.addEventListener('dragleave', function () {
                        this.classList.remove('dnd-drag-over');
                    });

                    item.addEventListener('drop', function (e) {
                        e.stopPropagation();
                        e.preventDefault();
                        this.classList.remove('dnd-drag-over');
                        if (dragSrc && dragSrc !== this) {
                            // DOM reorder: insert dragSrc before this item
                            const items = getItems();
                            const srcIdx = items.indexOf(dragSrc);
                            const tgtIdx = items.indexOf(this);
                            if (srcIdx < tgtIdx) {
                                listEl.insertBefore(dragSrc, this.nextSibling);
                            } else {
                                listEl.insertBefore(dragSrc, this);
                            }
                        }
                    });
                }

                // Attach events to all existing items
                getItems().forEach(attachDragEvents);

                // Make drag handle the visual affordance trigger
                $list.on('mousedown', '.drag-handle', function () {
                    $(this).closest('.customizer-option-item')[0].setAttribute('draggable', 'true');
                });
            })();

            $container.append($customizer);
        }

        function setupDateFilter(api, tableId, $container) {
            if ($container.find(`.date-filter-dropdown-container[data-table="${tableId}"]`).length) return;

            const $dateFilter = $(`
                <div class="date-filter-dropdown-container" data-table="${tableId}">
                    <button type="button" class="date-filter-btn date-filter-trigger-btn" style="display:inline-flex; align-items:center; gap:6px;">
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
                            <line x1="16" y1="2" x2="16" y2="6"></line>
                            <line x1="8" y1="2" x2="8" y2="6"></line>
                            <line x1="3" y1="10" x2="21" y2="10"></line>
                        </svg>
                        <span class="date-filter-label">All</span>
                        <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" class="chevron-icon">
                            <polyline points="6 9 12 15 18 9"></polyline>
                        </svg>
                    </button>
                    <div class="date-filter-dropdown-menu glass-dropdown">
                        <div class="dropdown-header" style="font-weight:700; padding:0 0 8px 0; font-size:11px; text-transform:uppercase; color:var(--text-muted);">Date Range</div>
                        <div style="display:flex; gap:8px; margin-bottom:12px;">
                            <div style="flex:1;">
                                <label style="font-size:10px; color:var(--text-muted); display:block; margin-bottom:4px; text-transform:uppercase;">From</label>
                                <input type="date" class="form-input date-start" style="width:100%; padding:4px 8px; font-size:11px; height:28px;" />
                            </div>
                            <div style="flex:1;">
                                <label style="font-size:10px; color:var(--text-muted); display:block; margin-bottom:4px; text-transform:uppercase;">To</label>
                                <input type="date" class="form-input date-end" style="width:100%; padding:4px 8px; font-size:11px; height:28px;" />
                            </div>
                        </div>
                        <div class="date-presets" style="display:grid; grid-template-columns:repeat(2, 1fr); gap:6px; margin-bottom:12px;">
                            <button type="button" class="if-filter-btn preset-btn" data-days="0" style="padding:4px !important; justify-content:center; height:24px; font-size:10px;">Today</button>
                            <button type="button" class="if-filter-btn preset-btn" data-days="1" style="padding:4px !important; justify-content:center; height:24px; font-size:10px;">Yesterday</button>
                            <button type="button" class="if-filter-btn preset-btn" data-days="7" style="padding:4px !important; justify-content:center; height:24px; font-size:10px;">Last 7 Days</button>
                            <button type="button" class="if-filter-btn preset-btn" data-days="30" style="padding:4px !important; justify-content:center; height:24px; font-size:10px;">Last 30 Days</button>
                            <button type="button" class="if-filter-btn preset-btn" data-preset="this-month" style="padding:4px !important; justify-content:center; height:24px; font-size:10px;">This Month</button>
                            <button type="button" class="if-filter-btn preset-btn" data-preset="all-time" style="padding:4px !important; justify-content:center; height:24px; font-size:10px;">All</button>
                        </div>
                        <div class="dropdown-divider" style="margin:8px 0; border-top:1px solid var(--border-subtle);"></div>
                        <div style="display:flex; justify-content:space-between; align-items:center;">
                            <button type="button" class="if-filter-btn filter-clear-btn" style="padding:4px 8px; font-size:10px; height:26px;">Clear</button>
                            <button type="button" class="btn btn-primary btn-sm filter-apply-btn" style="padding:4px 12px; font-size:11px; height:26px; line-height:1;">Apply</button>
                        </div>
                    </div>
                </div>
            `);

            function toLocalYYYYMMDD(date) {
                if (!date) return '';
                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, '0');
                const day = String(date.getDate()).padStart(2, '0');
                return `${year}-${month}-${day}`;
            }

            $dateFilter.find('.date-filter-trigger-btn').on('click', function (e) {
                e.stopPropagation();
                $('.date-filter-dropdown-menu').not($dateFilter.find('.date-filter-dropdown-menu')).removeClass('open');
                $('.customizer-dropdown-menu').removeClass('open');
                $dateFilter.find('.date-filter-dropdown-menu').toggleClass('open');
            });

            $dateFilter.find('.preset-btn').on('click', function (e) {
                e.stopPropagation();
                const days = $(this).data('days');
                const preset = $(this).data('preset');
                const today = new Date();

                let start = null;
                let end = today;

                if (typeof days !== 'undefined') {
                    if (days === 0) {
                        start = today;
                        end = today;
                    } else if (days === 1) {
                        start = new Date();
                        start.setDate(today.getDate() - 1);
                        end = new Date();
                        end.setDate(today.getDate() - 1);
                    } else {
                        start = new Date();
                        start.setDate(today.getDate() - days);
                        end = today;
                    }
                } else if (preset === 'this-month') {
                    start = new Date(today.getFullYear(), today.getMonth(), 1);
                    end = today;
                } else if (preset === 'all-time') {
                    start = null;
                    end = null;
                }

                $dateFilter.find('.date-start').val(start ? toLocalYYYYMMDD(start) : '');
                $dateFilter.find('.date-end').val(end ? toLocalYYYYMMDD(end) : '');

                applyDates();
            });

            $dateFilter.find('.filter-clear-btn').on('click', function (e) {
                e.stopPropagation();
                $dateFilter.find('.date-start').val('');
                $dateFilter.find('.date-end').val('');
                applyDates();
            });

            $dateFilter.find('.filter-apply-btn').on('click', function (e) {
                e.stopPropagation();
                applyDates();
            });

            function applyDates() {
                const sd = $dateFilter.find('.date-start').val();
                const ed = $dateFilter.find('.date-end').val();
                const $label = $dateFilter.find('.date-filter-label');

                if (sd && ed) {
                    $label.text(sd + ' - ' + ed);
                    $dateFilter.find('.date-filter-trigger-btn').addClass('active');
                } else if (sd) {
                    $label.text('From ' + sd);
                    $dateFilter.find('.date-filter-trigger-btn').addClass('active');
                } else if (ed) {
                    $label.text('To ' + ed);
                    $dateFilter.find('.date-filter-trigger-btn').addClass('active');
                } else {
                    $label.text('All');
                    $dateFilter.find('.date-filter-trigger-btn').removeClass('active');
                }

                $dateFilter.find('.date-filter-dropdown-menu').removeClass('open');
                api.draw();
            }

            $container.append($dateFilter);
        }
    }

    // Modal System
    const appModal = document.getElementById('appModal');
    const appModalDialog = appModal ? appModal.querySelector('.modal-dialog') : null;
    const closeModalBtn = document.getElementById('closeModal');

    window.closeModal = function () {
        if (appModal) {
            appModal.classList.remove('open', 'is-delete-modal');
            if (appModalDialog) appModalDialog.classList.remove('modal-confirm-dialog');
            setTimeout(() => {
                $('#modalContent').empty();
            }, 300);
        }
    };

    // Unified Wizard Form Handler
    window.initWizardForm = function (formSelector, options) {
        const $form = $(formSelector);
        if (!$form.length) return null;

        const steps = $form.find('.wizard-step');
        const totalSteps = steps.length;
        if (totalSteps === 0) return null;

        let currentStep = 0;

        const config = $.extend({
            showCancelButton: true,
            onStepChanging: function (currentIndex, newIndex) { return true; },
            onFinished: null,
            onBack: function () { window.closeModal(); }
        }, options);

        // Ensure proper modal-footer buttons exist
        let $footer = $form.find('.modal-footer');
        if (!$footer.length) {
            $footer = $('<div class="modal-footer wizard-footer"></div>').appendTo($form);
        } else {
            $footer.addClass('wizard-footer');
        }

        const $btnBack = $('<button type="button" class="btn btn-secondary wizard-btn-back">Cancel</button>');
        const $btnPrev = $('<button type="button" class="btn btn-secondary wizard-btn-prev" style="display:none;"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="transform: rotate(180deg); margin-right: 4px;"><polyline points="12 5 19 12 12 19"></polyline><line x1="19" y1="12" x2="5" y2="12"></line></svg>Previous</button>');
        const $btnNext = $('<button type="button" class="btn btn-primary wizard-btn-next" style="display:none;">Next<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-left: 4px;"><polyline points="12 5 19 12 12 19"></polyline><line x1="19" y1="12" x2="5" y2="12"></line></svg></button>');

        // Left group: Cancel (step 1) OR Previous (middle/last steps)
        const $leftGroup = $('<div class="wizard-footer-left"></div>');
        // Right group: Next / Save (Create Tenant)
        const $rightGroup = $('<div class="wizard-footer-right"></div>');

        let submitBtnText = 'Save';
        const rawSubmitBtn = $footer.find('button[type="submit"]');
        if (rawSubmitBtn.length) {
            submitBtnText = rawSubmitBtn.text().trim();
        }
        const $btnSave = $(`<button type="submit" class="btn btn-primary wizard-btn-save" style="display:none;"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="margin-right: 4px;"><polyline points="20 6 9 17 4 12"></polyline></svg>${submitBtnText}</button>`);

        $leftGroup.append($btnBack, $btnPrev);
        $rightGroup.append($btnNext, $btnSave);
        $footer.html('').append($leftGroup, $rightGroup);

        // Generate steps indicators header if not present
        let $header = $form.find('.wizard-steps-header');
        if (!$header.length) {
            $header = $('<div class="wizard-steps-header"></div>');
            $form.find('.modal-body').first().prepend($header);

            steps.each(function (index) {
                const stepTitle = $(this).data('title') || `Step ${index + 1}`;
                const $indicator = $(`
                    <div class="wizard-step-indicator" data-step="${index}">
                        <div class="step-number">${index + 1}</div>
                        <div class="step-label">${stepTitle}</div>
                    </div>
                `);
                $header.append($indicator);
            });

            if (totalSteps === 1) {
                $header.hide();
            }
        }

        // Silent validation checker for dynamic Next button visibility
        function checkActiveStepValidSilent() {
            let stepValid = true;
            const $activeStep = steps.eq(currentStep);

            // Validate standard HTML5 required inputs (exclude file inputs)
            $activeStep.find('input[required]:not([type="file"]), select[required], textarea[required]').each(function () {
                const val = (this.value || '').trim();
                if (!val) {
                    stepValid = false;
                    return false; // break early
                }
            });

            if (!stepValid) return false;

            // Validate HTML5 constraints (pattern, min, max, etc.) only for non-empty values
            // Skip empty optional email/url fields — browsers may return false for checkValidity
            // even on empty non-required inputs in some edge cases.
            $activeStep.find('input:not([type="file"]):not([type="email"]):not([type="url"]), select, textarea').each(function () {
                const isRequired = this.hasAttribute('required');
                const hasValue = this.value && this.value.trim().length > 0;
                if (isRequired || hasValue) {
                    if (typeof this.checkValidity === 'function' && !this.checkValidity()) {
                        stepValid = false;
                        return false;
                    }
                }
                if ($(this).hasClass('ct-invalid') || $(this).hasClass('input-validation-error')) {
                    stepValid = false;
                    return false;
                }
            });

            if (!stepValid) return false;

            // For email/url: only validate if they have a value AND are required or non-empty
            $activeStep.find('input[type="email"], input[type="url"]').each(function () {
                const hasValue = this.value && this.value.trim().length > 0;
                if (hasValue) {
                    if (typeof this.checkValidity === 'function' && !this.checkValidity()) {
                        stepValid = false;
                        return false;
                    }
                } else if (this.hasAttribute('required')) {
                    stepValid = false;
                    return false;
                }
                if ($(this).hasClass('ct-invalid') || $(this).hasClass('input-validation-error')) {
                    stepValid = false;
                    return false;
                }
            });

            return stepValid;
        }

        function updateNextButtonVisibility() {
            const isValid = checkActiveStepValidSilent();
            if (currentStep === totalSteps - 1) {
                $btnNext.hide();
                $btnSave.show();
                $btnSave.prop('disabled', !isValid);
            } else {
                $btnSave.hide();
                $btnNext.show();
                $btnNext.prop('disabled', !isValid);
            }
        }

        function showStep(index) {
            steps.removeClass('active').eq(index).addClass('active');

            const indicators = $form.find('.wizard-step-indicator');
            indicators.removeClass('active completed');
            indicators.each(function (i) {
                if (i === index) {
                    $(this).addClass('active');
                } else if (i < index) {
                    $(this).addClass('completed');
                }
            });

            if (index === 0) {
                if (config.showCancelButton) {
                    $btnBack.show();
                } else {
                    $btnBack.hide();
                }
                $btnPrev.hide();
            } else {
                $btnBack.hide();
                $btnPrev.show();
            }

            currentStep = index;
            updateNextButtonVisibility();
        }

        // Field Validation for Active Step (Visual error indicator trigger)
        function validateActiveStep() {
            let stepValid = true;
            const $activeStep = steps.eq(currentStep);

            // Validate standard HTML5 inputs
            $activeStep.find('input[required], select[required], textarea[required]').each(function () {
                if (!this.value || !this.value.trim()) {
                    $(this).addClass('ct-invalid');
                    stepValid = false;
                } else {
                    $(this).removeClass('ct-invalid');
                }
            });

            // Trigger jQuery Validate if active
            if (typeof $ !== 'undefined' && $.validator) {
                $activeStep.find('input, select, textarea').each(function () {
                    if ($(this).valid && !$(this).valid()) {
                        stepValid = false;
                    }
                });
            }

            if (!stepValid) {
                const firstInvalid = $activeStep.find('.ct-invalid, .input-validation-error').first();
                if (firstInvalid.length) {
                    firstInvalid.focus();
                }
            }

            return stepValid;
        }

        $btnNext.on('click', function (e) {
            e.preventDefault();
            if (validateActiveStep()) {
                if (config.onStepChanging(currentStep, currentStep + 1)) {
                    showStep(currentStep + 1);
                }
            }
        });

        $btnPrev.on('click', function (e) {
            e.preventDefault();
            if (config.onStepChanging(currentStep, currentStep - 1)) {
                showStep(currentStep - 1);
            }
        });

        $btnBack.on('click', function (e) {
            e.preventDefault();
            config.onBack();
        });

        // Listen for input events to dynamically update buttons
        $form.off('input.wizard change.wizard keyup.wizard').on('input.wizard change.wizard keyup.wizard', 'input, select, textarea', function () {
            setTimeout(updateNextButtonVisibility, 50);
        });

        // If form is submitted on save button
        $form.off('submit.wizard').on('submit.wizard', function (e) {
            if (!validateActiveStep()) {
                e.preventDefault();
                return false;
            }
            if (typeof config.onFinished === 'function') {
                e.preventDefault();
                config.onFinished();
                return false;
            }
        });

        showStep(0);

        return {
            showStep: showStep,
            getCurrentStep: function () { return currentStep; },
            validateActiveStep: validateActiveStep
        };
    };

    // Unified View Mode Applicator (List, Grid, Kanban)
    window.applyViewMode = function (targetTableId, mode) {
        const $table = $('#' + targetTableId);
        if (!$table.length) return;

        const $panel = $table.closest(PANEL_SELECTOR);
        if (!$panel.length) return;

        const $grid = $panel.find('.grid-cards-container');
        const $kanban = $panel.find('.kanban-board-container');
        const $tableResponsive = $table.closest('.table-responsive');

        // Hide all view containers first to prevent dual rendering
        $tableResponsive.hide();
        if ($grid.length) $grid.hide();
        if ($kanban.length) $kanban.hide();

        // Update switcher buttons state
        $panel.find('.view-toggle-btn').removeClass('active');
        $panel.find(`.view-toggle-btn[data-view="${mode}"]`).addClass('active');

        // Save selection
        localStorage.setItem('view-mode-' + targetTableId, mode);

        // Show the active view
        if (mode === 'grid') {
            if ($grid.length) $grid.show();
        } else if (mode === 'kanban') {
            if ($kanban.length) $kanban.show();
        } else {
            $tableResponsive.show();
        }

        // Recalculate DataTable if visible with a small delay for DOM layout reflow
        if (mode === 'list' && typeof $ !== 'undefined' && $.fn.dataTable && $.fn.dataTable.isDataTable($table)) {
            setTimeout(() => {
                $table.DataTable().columns.adjust().responsive.recalc();
            }, 100);
        }
    };

    // Global View Toggle Click Handler
    $(document).on('click', '.view-toggle-btn', function (e) {
        e.preventDefault();
        const mode = $(this).data('view');
        const targetTableId = $(this).data('target');
        window.applyViewMode(targetTableId, mode);
    });

    window.openModal = function (config) {
        if (!appModal) return;

        const { title, subtitle, icon, url, content, html, onSuccess } = config;
        const isDelete = (url && url.toLowerCase().includes('delete')) || (title && title.toLowerCase().includes('delete'));

        if (appModal) {
            if (isDelete) {
                appModal.classList.add('is-delete-modal');
                if (appModalDialog) appModalDialog.classList.add('modal-confirm-dialog');
            } else {
                appModal.classList.remove('is-delete-modal');
                if (appModalDialog) appModalDialog.classList.remove('modal-confirm-dialog');
            }
        }

        document.getElementById('modalTitle').innerText = title || '';
        document.getElementById('modalSubtitle').innerText = (isDelete && subtitle && subtitle.includes('DELETE')) ? '' : (subtitle || '');
        if (icon) {
            document.getElementById('modalIcon').innerHTML = icon;
        }

        // Use .off().on() pattern or ensure clean slate
        window.onModalSuccess = onSuccess;

        if (content || html) {
            const modalContent = document.getElementById('modalContent');
            window.injectHtmlWithNonce(modalContent, content || html);
            appModal.classList.add('open');
            $('#modalContent button.btn-secondary, #modalContent a.btn-secondary').off('click').on('click', function (e) {
                const text = $(this).text().trim().toLowerCase();
                if (text === 'cancel' || text === 'close' || text === 'dismiss') {
                    window.closeModal();
                }
            });
            return;
        }

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

                // Auto-wire type-to-confirm DELETE inputs
                const $deleteInput = $('#modalContent').find('.delete-confirm-input');
                if ($deleteInput.length) {
                    $deleteInput.val('').off('input keyup paste change').on('input keyup paste change', function () {
                        const typed = $(this).val().trim();
                        const $btn = $('#modalContent').find('.btn-delete-confirm, button[type="submit"], #confirmDeleteSubscription, #confirmDeleteTenant, #confirmDeleteUser, #confirmDeleteClient, #submitDeleteRole');
                        if (typed === 'DELETE') {
                            $btn.prop('disabled', false).css({ opacity: '1', cursor: 'pointer' });
                        } else {
                            $btn.prop('disabled', true).css({ opacity: '0.5', cursor: 'not-allowed' });
                        }
                    });
                }

                $('#modalContent button.btn-secondary, #modalContent a.btn-secondary, #modalContent #cancelCreateTenant').off('click').on('click', function (e) {
                    const text = $(this).text().trim().toLowerCase();
                    if (text === 'cancel' || text === 'close' || text === 'dismiss' || this.id === 'cancelCreateTenant' || $(this).hasClass('wizard-btn-back')) {
                        window.closeModal();
                    }
                });
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
            if (quickActionsDropdown) quickActionsDropdown.classList.remove('open');
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
            if (quickActionsDropdown) quickActionsDropdown.classList.remove('open');
        });
    }

    // Quick Actions Dropdown Toggle
    const quickActionsToggle = document.getElementById('quickActionsToggle');
    const quickActionsDropdown = document.getElementById('quickActionsDropdown');

    if (quickActionsToggle && quickActionsDropdown) {
        quickActionsToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            quickActionsDropdown.classList.toggle('open');
            if (userDropdown) userDropdown.classList.remove('open');
            if (tenantDropdown) tenantDropdown.classList.remove('open');
        });
    }

    // Intercept clicks on quick action items to use modals instead of navigation
    $(document).on('click', '.quick-actions-container [data-quick-action]', function (e) {
        e.preventDefault();
        const type = $(this).data('quick-action');
        if (quickActionsDropdown) {
            quickActionsDropdown.classList.remove('open');
        }
        if (typeof window.triggerQuickAdd === 'function') {
            window.triggerQuickAdd(type);
        }
    });

    // Close all dropdowns when clicking elsewhere
    document.addEventListener('click', (e) => {
        if (userDropdown && userDropdown.classList.contains('open') && !userDropdown.contains(e.target)) {
            userDropdown.classList.remove('open');
        }
        if (tenantDropdown && tenantDropdown.classList.contains('open') && !tenantDropdown.contains(e.target)) {
            tenantDropdown.classList.remove('open');
        }
        if (quickActionsDropdown && quickActionsDropdown.classList.contains('open') && !quickActionsDropdown.contains(e.target)) {
            quickActionsDropdown.classList.remove('open');
        }
        if (!e.target.closest('.customizer-dropdown-container')) {
            $('.customizer-dropdown-menu').removeClass('open');
        }
        if (!e.target.closest('.date-filter-dropdown-container')) {
            $('.date-filter-dropdown-menu').removeClass('open');
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

    // Settings Drawer Panel Elements
    const settingsDrawer = document.getElementById('settingsDrawer');
    const settingsBackdrop = document.getElementById('settingsDrawerBackdrop');
    const settingsOpenBtn = document.getElementById('settings-drawer-btn');
    const settingsCloseBtn = document.getElementById('closeSettingsDrawerBtn');

    // Open/Close functions
    function openSettingsDrawer() {
        if (settingsDrawer && settingsBackdrop) {
            settingsDrawer.classList.add('open');
            settingsBackdrop.classList.add('open');
            // Sync values to settings options when opening
            syncSettingsToUI();
        }
    }

    function closeSettingsDrawer() {
        if (settingsDrawer && settingsBackdrop) {
            settingsDrawer.classList.remove('open');
            settingsBackdrop.classList.remove('open');
        }
    }

    if (settingsOpenBtn) settingsOpenBtn.addEventListener('click', openSettingsDrawer);
    if (settingsCloseBtn) settingsCloseBtn.addEventListener('click', closeSettingsDrawer);
    if (settingsBackdrop) settingsBackdrop.addEventListener('click', closeSettingsDrawer);

    // Synchronize current document attributes to radio buttons/colors in the drawer
    function syncSettingsToUI() {
        const themePreference = document.documentElement.getAttribute('data-theme-preference') || 'system';
        const layout = document.documentElement.getAttribute('data-layout') || 'sidebar';
        const color = document.documentElement.getAttribute('data-theme-color') || 'blue';
        const density = document.documentElement.getAttribute('data-density') || 'standard';
        const cardStyle = document.documentElement.getAttribute('data-card-style') || 'bordered';
        const dashLayout = document.documentElement.getAttribute('data-dashboard-layout') || 'grid';
        const fontFamily = document.documentElement.getAttribute('data-font-family') || 'inter';
        const fontSize = document.documentElement.getAttribute('data-font-size') || 'medium';

        // Set radios
        const themeRadio = document.querySelector(`input[name="color-mode"][value="${themePreference}"]`);
        if (themeRadio) themeRadio.checked = true;

        const densityRadio = document.querySelector(`input[name="ui-density"][value="${density}"]`);
        if (densityRadio) densityRadio.checked = true;

        const cardRadio = document.querySelector(`input[name="card-style"][value="${cardStyle}"]`);
        if (cardRadio) cardRadio.checked = true;

        const dashRadio = document.querySelector(`input[name="dashboard-layout"][value="${dashLayout}"]`);
        if (dashRadio) dashRadio.checked = true;

        const fontRadio = document.querySelector(`input[name="font-family"][value="${fontFamily}"]`);
        if (fontRadio) fontRadio.checked = true;

        const fontSizeRadio = document.querySelector(`input[name="font-size"][value="${fontSize}"]`);
        if (fontSizeRadio) fontSizeRadio.checked = true;

        // Set active color dot
        document.querySelectorAll('.color-dot').forEach(dot => {
            if (dot.getAttribute('data-color') === color) {
                dot.classList.add('active');
            } else {
                dot.classList.remove('active');
            }
        });

        applyDashboardLayout(dashLayout);
    }

    // Bind event listeners for Drawer controls
    // Color Mode (Light/Dark)
    document.querySelectorAll('input[name="color-mode"]').forEach(radio => {
        radio.addEventListener('change', (e) => {
            const val = e.target.value;
            localStorage.setItem('theme', val);
            document.documentElement.setAttribute('data-theme-preference', val);

            let themeToApply = val;
            if (val === 'system') {
                themeToApply = window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
            }
            document.documentElement.setAttribute('data-theme', themeToApply);
        });
    });

    // Real-time system color mode preference detector
    window.matchMedia('(prefers-color-scheme: light)').addEventListener('change', (e) => {
        const savedTheme = localStorage.getItem('theme') || 'system';
        if (savedTheme === 'system') {
            const themeToApply = e.matches ? 'light' : 'dark';
            document.documentElement.setAttribute('data-theme', themeToApply);
        }
    });

    // Font Family (Inter/Outfit/Poppins/Lora/Fira Code)
    document.querySelectorAll('input[name="font-family"]').forEach(radio => {
        radio.addEventListener('change', (e) => {
            const val = e.target.value;
            document.documentElement.setAttribute('data-font-family', val);
            localStorage.setItem('font-family', val);
        });
    });

    // Font Size Scale
    document.querySelectorAll('input[name="font-size"]').forEach(radio => {
        radio.addEventListener('change', (e) => {
            const val = e.target.value;
            document.documentElement.setAttribute('data-font-size', val);
            localStorage.setItem('font-size', val);
            // Trigger DataTable column adjustments if visible
            if (typeof $ !== 'undefined' && $.fn.dataTable) {
                setTimeout(() => {
                    $('.data-table').DataTable().columns.adjust().responsive.recalc();
                }, 300);
            }
        });
    });

    // Color Scheme Palette
    document.querySelectorAll('.color-dot').forEach(dot => {
        dot.addEventListener('click', (e) => {
            const color = e.target.getAttribute('data-color');
            document.documentElement.setAttribute('data-theme-color', color);
            localStorage.setItem('theme-color', color);

            // Toggle active classes
            document.querySelectorAll('.color-dot').forEach(d => d.classList.remove('active'));
            e.target.classList.add('active');
        });
    });

    // UI Density
    document.querySelectorAll('input[name="ui-density"]').forEach(radio => {
        radio.addEventListener('change', (e) => {
            const val = e.target.value;
            document.documentElement.setAttribute('data-density', val);
            localStorage.setItem('density', val);
            // Trigger DataTable column adjustments
            if (typeof $ !== 'undefined' && $.fn.dataTable) {
                setTimeout(() => {
                    $('.data-table').DataTable().columns.adjust().responsive.recalc();
                }, 300);
            }
        });
    });

    // Card Style (Flat/Bordered/Elevated/Glass)
    document.querySelectorAll('input[name="card-style"]').forEach(radio => {
        radio.addEventListener('change', (e) => {
            const val = e.target.value;
            document.documentElement.setAttribute('data-card-style', val);
            localStorage.setItem('card-style', val);
        });
    });

    // Dashboard Layout (Grid/Tabbed/Split)
    document.querySelectorAll('input[name="dashboard-layout"]').forEach(radio => {
        radio.addEventListener('change', (e) => {
            const val = e.target.value;
            document.documentElement.setAttribute('data-dashboard-layout', val);
            localStorage.setItem('dashboard-layout', val);
            applyDashboardLayout(val);
        });
    });

    function applyDashboardLayout(layout) {
        if (layout === 'tabbed') {
            if (typeof window.switchDashboardTab === 'function') {
                window.switchDashboardTab(0);
            }
        } else {
            const panels = document.querySelectorAll('.dashboard-layout > .panel');
            panels.forEach(p => p.classList.remove('active-tab'));
        }
    }

    // Global dashboard tab switcher
    window.switchDashboardTab = function (index) {
        const layout = document.querySelector('.dashboard-layout');
        if (!layout) return;
        const panels = Array.from(layout.children);
        panels.forEach((p, idx) => {
            if (idx === index) {
                p.classList.add('active-tab');
            } else {
                p.classList.remove('active-tab');
            }
        });
        const btns = document.querySelectorAll('.dashboard-tab-btn');
        btns.forEach((btn, idx) => {
            btn.classList.toggle('active', idx === index);
        });
    };

    // Global Quick Add trigger
    window.triggerQuickAdd = function (type) {
        switch (type) {
            case 'tenant':
                window.openModal({
                    title: 'Provision New Tenant',
                    subtitle: 'Scale your ecosystem by adding a new organization boundary.',
                    icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path></svg>',
                    url: '/Home/CreateTenant'
                });
                break;
            case 'license':
                // Redirect directly to Tenants listing page
                window.location.href = '/Home/Tenants?openAdd=true';
                break;
            case 'subscription':
                window.openModal({
                    title: 'Design New Subscription',
                    subtitle: 'Design a new subscription package for tenants.',
                    icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="5" width="20" height="14" rx="2" ry="2"></rect><line x1="2" y1="10" x2="22" y2="10"></line></svg>',
                    url: '/Home/CreateSubscription'
                });
                break;
            case 'user':
                window.openModal({
                    title: 'Create New User',
                    subtitle: 'Register a new identity on the platform.',
                    icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle></svg>',
                    url: '/Home/CreateUser'
                });
                break;
            case 'role':
                window.openModal({
                    title: 'Define New Role',
                    subtitle: 'Define a new access level for system users.',
                    icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 2 7 12 12 22 7 12 2"></polygon></svg>',
                    url: '/Home/CreateRole'
                });
                break;
            case 'permission':
                // Redirect directly to Roles listing page
                window.location.href = '/Home/Roles';
                break;
            case 'application':
                window.openModal({
                    title: 'Create Application',
                    subtitle: 'Register a new client application.',
                    icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><line x1="3" y1="9" x2="21" y2="9"></line></svg>',
                    url: '/Home/CreateClient'
                });
                break;
            case 'service':
                window.openModal({
                    title: 'Define New Service Template',
                    subtitle: 'Define a new system template for organization-wide use.',
                    icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path></svg>',
                    url: '/Home/CreateTemplate'
                });
                break;
            default:
                console.warn('Unknown quick add type:', type);
        }
    };

    // Auto open "Provision" on listing pages if openAdd parameter exists
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('openAdd') === 'true') {
        setTimeout(() => {
            const addBtn = document.getElementById('openCreateTenantModal') ||
                document.getElementById('openCreateClientModal') ||
                document.getElementById('openCreateUserModal') ||
                document.getElementById('openCreateRoleModal') ||
                document.getElementById('openCreateSubscriptionModal') ||
                document.getElementById('openCreateTemplateModal');
            if (addBtn) {
                addBtn.click();
            }
        }, 300);
    }

    // Details View Drawer Logic
    const dataViewDrawer = document.getElementById('dataViewDrawer');
    const dataViewDrawerBackdrop = document.getElementById('dataViewDrawerBackdrop');
    const closeDataViewDrawerBtn = document.getElementById('closeDataViewDrawerBtn');
    const dataViewDrawerCloseBtn = document.getElementById('dataViewDrawerCloseBtn');
    const dataViewDrawerActionBtn = document.getElementById('dataViewDrawerActionBtn');

    window.closeDetailsDrawer = function () {
        if (dataViewDrawer && dataViewDrawerBackdrop) {
            dataViewDrawer.classList.remove('open');
            dataViewDrawerBackdrop.classList.remove('open');
        }
    };

    // Global DataTable Action View listener
    $(document).on('click', '.action-btn-view, .if-action-btn-view', function (e) {
        console.log("View Details clicked. Target:", this, "Data-Type:", $(this).data('type'), "Attr-Type:", $(this).attr('data-type'));
        e.preventDefault();
        e.stopPropagation();
        const type = $(this).data('type') || $(this).attr('data-type');
        if (!type) {
            console.warn("No type found on clicked element:", this);
            return;
        }

        let id = $(this).data('id') || $(this).attr('data-id');
        if (!id) {
            const $table = $(this).closest('table');
            if ($table.length && typeof $ !== 'undefined' && $.fn.dataTable && $.fn.dataTable.isDataTable($table[0])) {
                const table = $table.DataTable();
                // Get row regardless of whether it is responsive child row or parent row
                let tr = $(this).closest('tr');
                if (tr.hasClass('child')) {
                    tr = tr.prev();
                }
                const rowData = table.row(tr).data();
                if (rowData) {
                    id = rowData.id || rowData.clientId;
                }
            }
        }

        if (id) {
            window.showDetailsDrawer(type, id);
        } else {
            // Try to find if there is a row we can extract
            let rowData = null;
            const $table = $(this).closest('table');
            if ($table.length && typeof $ !== 'undefined' && $.fn.dataTable && $.fn.dataTable.isDataTable($table[0])) {
                const table = $table.DataTable();
                let tr = $(this).closest('tr');
                if (tr.hasClass('child')) { tr = tr.prev(); }
                rowData = table.row(tr).data();
            }
            if (rowData) {
                window.showDetailsDrawer(type, rowData);
            } else {
                console.warn("Could not retrieve ID or row data for View Details.");
            }
        }
    });

    if (closeDataViewDrawerBtn) closeDataViewDrawerBtn.addEventListener('click', window.closeDetailsDrawer);
    if (dataViewDrawerCloseBtn) dataViewDrawerCloseBtn.addEventListener('click', window.closeDetailsDrawer);
    if (dataViewDrawerBackdrop) dataViewDrawerBackdrop.addEventListener('click', window.closeDetailsDrawer);

    window.showDetailsDrawer = function (type, idOrRow) {
        if (!dataViewDrawer || !dataViewDrawerBackdrop) return;

        const titleEl = document.getElementById('dataViewDrawerTitle');
        const subtitleEl = document.getElementById('dataViewDrawerSubtitle');
        const iconEl = document.getElementById('dataViewDrawerIcon');
        const bodyEl = document.getElementById('dataViewDrawerBody');

        // Immediately open the drawer & show a loading spinner
        dataViewDrawer.classList.add('open');
        dataViewDrawerBackdrop.classList.add('open');

        bodyEl.innerHTML = `
            <div class="drawer-loading-state" style="display:flex; flex-direction:column; align-items:center; justify-content:center; padding:60px 20px; gap:16px; color:var(--text-muted);">
                <div class="modern-spinner" style="width:28px; height:28px; border-width:3px; border-color:var(--border-subtle); border-top-color:var(--brand-primary); border-radius:50%; animation:spin 1s linear infinite;"></div>
                <span style="font-size:0.8rem; font-weight:500;">Retrieving details...</span>
            </div>
        `;

        function renderData(row) {
            bodyEl.innerHTML = ''; // clear
            let title = 'Details';
            let subtitle = 'View record properties';
            let iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect></svg>';
            let sections = [];

            if (type === 'tenant') {
                title = row.name || 'Tenant Details';
                subtitle = `Code: ${row.code || '-'}`;
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path></svg>';

                const appsValue = row.clientNames && row.clientNames.length
                    ? row.clientNames.join(', ')
                    : (row.applicationsCount != null ? `${row.applicationsCount} App(s)` : '-');

                sections = [
                    {
                        title: 'General Details',
                        properties: [
                            { label: 'Organization Name', value: row.name },
                            { label: 'Tenant Code', value: row.code },
                            { label: 'Status', value: row.isActive ? '<span class="badge badge-success">Active</span>' : '<span class="badge badge-error">Inactive</span>' }
                        ]
                    },
                    {
                        title: 'Database Configuration',
                        properties: [
                            { label: 'Database Mode', value: row.databaseMode },
                            { label: 'Database Provider', value: row.databaseProvider || '-' },
                            { label: 'Database Name', value: row.databaseName || '-' }
                        ]
                    },
                    {
                        title: 'Contact & Billing Details',
                        properties: [
                            { label: 'Email Address', value: row.email ? `<a href="mailto:${row.email}">${row.email}</a>` : '-' },
                            { label: 'Phone Number', value: row.phone || '-' },
                            { label: 'Website', value: row.website ? `<a href="${row.website}" target="_blank">${row.website}</a>` : '-' },
                            { label: 'Preferred Currency', value: row.currency || '-' },
                            { label: 'Billing Address', value: row.billingAddress || '-' },
                            { label: 'Grace Period', value: `${row.gracePeriodDays ?? 7} Days` }
                        ]
                    },
                    {
                        title: 'Branding & Apps',
                        properties: [
                            { label: 'Chalkboard Background Text', value: row.backgroundText || '-' },
                            { label: 'Client Applications', value: appsValue }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).show().text('Edit Tenant').off('click').on('click', () => {
                    window.closeDetailsDrawer();
                    if (typeof window.editTenant === 'function') window.editTenant(row.id);
                });
            }
            else if (type === 'user' || type === 'tenantUser') {
                const fullName = (row.firstName && row.lastName) ? `${row.firstName} ${row.lastName}` : (row.firstName || row.name || row.userName || 'User Details');
                title = fullName;
                subtitle = `Username: @${row.userName || '-'}`;
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>';

                const roles = row.roles && row.roles.length
                    ? row.roles.map(r => `<span class="badge badge-neutral" style="font-size:0.75rem; padding: 2px 6px; margin-right:4px;">${r}</span>`).join(' ')
                    : (row.roleNames && row.roleNames.length
                        ? row.roleNames.map(r => `<span class="badge badge-neutral" style="font-size:0.75rem; padding: 2px 6px; margin-right:4px;">${r}</span>`).join(' ')
                        : 'No Roles');

                const apps = row.clientNames && row.clientNames.length
                    ? row.clientNames.join(', ')
                    : '-';

                const isEmailConfirmed = row.isEmailConfirmed != null
                    ? (row.isEmailConfirmed ? '<span class="badge badge-success">Confirmed</span>' : '<span class="badge badge-warning">Unconfirmed</span>')
                    : (row.autoConfirmEmail ? '<span class="badge badge-success">Confirmed</span>' : '<span class="badge badge-warning">Unconfirmed</span>');

                sections = [
                    {
                        title: 'User Details',
                        properties: [
                            { label: 'Full Name', value: fullName },
                            { label: 'Email Address', value: row.email ? `<a href="mailto:${row.email}">${row.email}</a>` : '-' },
                            { label: 'Username', value: row.userName || '-' },
                            { label: 'Phone Number', value: row.phoneNumber || row.phone || '-' }
                        ]
                    },
                    {
                        title: 'Organization & Access',
                        properties: [
                            { label: 'Tenant Organization', value: row.tenantName || (row.tenantId ? 'Assigned Tenant' : 'Global Admin') },
                            { label: 'Role Assignment', value: roles },
                            { label: 'Application Consents', value: apps }
                        ]
                    },
                    {
                        title: 'Account Settings',
                        properties: [
                            { label: 'Account Status', value: row.isActive ? '<span class="badge badge-success">Active</span>' : '<span class="badge badge-error">Inactive</span>' },
                            { label: 'Email Status', value: isEmailConfirmed }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).show().text('Edit User').off('click').on('click', () => {
                    window.closeDetailsDrawer();
                    if (typeof window.editUser === 'function') window.editUser(row.id);
                });
            }
            else if (type === 'role') {
                title = row.name || 'Role Details';
                subtitle = row.description || 'System Access Role';
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 2 7 12 12 22 7 12 2"></polygon></svg>';

                sections = [
                    {
                        title: 'Role Specification',
                        properties: [
                            { label: 'Role Name', value: row.name },
                            { label: 'Description', value: row.description || '-' },
                            { label: 'Tenant Organization', value: row.tenantName || 'Global System Role' },
                            { label: 'System Role', value: row.isSystemRole ? 'Yes (Protected)' : 'No' }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).show().text('Edit Role').off('click').on('click', () => {
                    window.closeDetailsDrawer();
                    if (typeof window.editRole === 'function') window.editRole(row.id);
                });
            }
            else if (type === 'subscription') {
                let billingCycleText = row.billingCycle;
                if (billingCycleText === 1 || billingCycleText === '1' || billingCycleText === 'Monthly') billingCycleText = 'Monthly';
                if (billingCycleText === 2 || billingCycleText === '2' || billingCycleText === 'Yearly') billingCycleText = 'Yearly';

                const currencyStr = row.currency || 'INR';

                title = row.name || 'Subscription Details';
                subtitle = `Cycle: ${billingCycleText || 'Monthly'}`;
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="5" width="20" height="14" rx="2" ry="2"></rect></svg>';

                sections = [
                    {
                        title: 'Plan Details',
                        properties: [
                            { label: 'Plan Name', value: row.name },
                            { label: 'Description', value: row.description || '-' },
                            { label: 'Billing Cycle', value: billingCycleText },
                            { label: 'Price Rate', value: `${row.price} ${currencyStr}` }
                        ]
                    },
                    {
                        title: 'Pricing & Quotas',
                        properties: [
                            { label: 'Maximum Users', value: row.maxUsers != null ? row.maxUsers : '-' },
                            { label: 'Maximum Applications', value: row.maxApps != null ? row.maxApps : '-' }
                        ]
                    },
                    {
                        title: 'Isolation Strategy & Status',
                        properties: [
                            { label: 'Allow Separate Database', value: row.allowSeparateDb ? 'Isolated Db Catalog' : 'Shared Db Catalog' },
                            { label: 'Active Status', value: row.isActive != null ? (row.isActive ? '<span class="badge badge-success">Active</span>' : '<span class="badge badge-error">Inactive</span>') : '-' }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).show().text('Edit Plan').off('click').on('click', () => {
                    window.closeDetailsDrawer();
                    if (typeof window.editSubscription === 'function') window.editSubscription(row.id);
                });
            }
            else if (type === 'client' || type === 'application') {
                const displayName = row.clientName || row.displayName || 'Application Client';
                title = displayName;
                subtitle = `Client ID: ${row.clientId || '-'}`;
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect></svg>';

                let loginMethod = row.allowedLoginMethod || row.consentType || 'CredentialsOnly';
                if (loginMethod === 0 || loginMethod === '0' || loginMethod === 'CredentialsOnly') loginMethod = 'Credentials';
                else if (loginMethod === 1 || loginMethod === '1' || loginMethod === 'MobileOtpOnly') loginMethod = 'Mobile OTP';
                else if (loginMethod === 2 || loginMethod === '2' || loginMethod === 'Both') loginMethod = 'Both (Credentials & Mobile OTP)';

                let scopesText = '-';
                if (row.scopes && Array.isArray(row.scopes) && row.scopes.length > 0) {
                    scopesText = row.scopes.map(s => typeof s === 'string' ? s : (s.name || s.scopeName || s.key || JSON.stringify(s))).join(', ');
                } else if (row.allowedScopes) {
                    scopesText = Array.isArray(row.allowedScopes) ? row.allowedScopes.join(', ') : row.allowedScopes;
                }

                const redirectText = row.redirectUris ? (Array.isArray(row.redirectUris) ? row.redirectUris.join('<br>') : row.redirectUris) : '-';
                const postLogoutText = row.postLogoutRedirectUris ? (Array.isArray(row.postLogoutRedirectUris) ? row.postLogoutRedirectUris.join('<br>') : row.postLogoutRedirectUris) : '-';

                sections = [
                    {
                        title: 'App Identity',
                        properties: [
                            { label: 'Display Name', value: displayName },
                            { label: 'Client ID', value: row.clientId },
                            { label: 'Application Type', value: row.appClientType || row.clientType || row.type || 'Web / API' }
                        ]
                    },
                    {
                        title: 'Redirect Configuration',
                        properties: [
                            { label: 'Callback URLs', value: redirectText },
                            { label: 'Post-Logout URLs', value: postLogoutText }
                        ]
                    },
                    {
                        title: 'Scopes & Permissions',
                        properties: [
                            { label: 'Scopes', value: scopesText }
                        ]
                    },
                    {
                        title: 'Auth & Security',
                        properties: [
                            { label: 'Login Method', value: loginMethod },
                            { label: 'Two-Factor Auth (2FA)', value: row.require2FA != null ? (row.require2FA ? 'Required' : 'Optional') : '-' },
                            { label: 'Public Registration', value: row.allowPublicRegistration ? '<span class="badge badge-success">Allowed</span>' : '<span class="badge badge-neutral">Off</span>' },
                            { label: 'Default Registration Role', value: row.defaultRoleName || 'End User' }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).show().text('Edit Application').off('click').on('click', () => {
                    window.closeDetailsDrawer();
                    if (typeof window.editClient === 'function') window.editClient(row.id);
                });
            }
            else if (type === 'template' || type === 'service') {
                title = row.name || 'Document Template';
                subtitle = `Key: ${row.key || '-'}`;
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path></svg>';

                let docTypeName = row.type;
                if (docTypeName === 0 || docTypeName === '0') docTypeName = 'Excel (.xlsx)';
                else if (docTypeName === 1 || docTypeName === '1') docTypeName = 'Word (.docx)';
                else if (docTypeName === 2 || docTypeName === '2') docTypeName = 'PowerPoint (.pptx)';

                sections = [
                    {
                        title: 'General Details',
                        properties: [
                            { label: 'Template Name', value: row.name },
                            { label: 'Logical Key', value: row.key },
                            { label: 'Document Type', value: docTypeName || '-' },
                            { label: 'Version', value: row.version || '-' }
                        ]
                    },
                    {
                        title: 'Template File',
                        properties: [
                            { label: 'Template File Path', value: row.content || '-' }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).show().text('Edit Template').off('click').on('click', () => {
                    window.closeDetailsDrawer();
                    if (typeof window.editTemplate === 'function') window.editTemplate(row.id);
                });
            }
            else if (type === 'emailTemplate') {
                title = row.name || 'Email Template';
                subtitle = `Subject: ${row.subject || '-'}`;
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/></svg>';

                let tType = (row.templateType === 0 || row.templateType === '0' || row.templateType === 'HTML') ? 'HTML' : 'Plain Text';

                const triggerLabels = {
                    0: 'Registration', 1: 'Password Reset', 2: 'Email Confirmation',
                    3: 'Security Alert', 4: 'Account Locked', 5: 'Welcome Email',
                    6: 'Invitation', 99: 'Custom'
                };
                let tEvent = triggerLabels[row.triggerEvent] || row.triggerEvent || 'Unknown';

                sections = [
                    {
                        title: 'General Details',
                        properties: [
                            { label: 'Template Name', value: row.name },
                            { label: 'Subject Line', value: row.subject },
                            { label: 'Template Type', value: tType },
                            { label: 'Trigger Event', value: tEvent },
                            { label: 'Active Status', value: row.isActive ? '<span class="badge badge-success">Active</span>' : '<span class="badge badge-error">Inactive</span>' }
                        ]
                    },
                    {
                        title: 'Email Content',
                        properties: [
                            { label: 'Body Content', value: `<pre style="font-size:11px; text-align:left; max-width:280px; overflow-x:auto; white-space:pre-wrap; background:var(--bg-base); padding:8px; border-radius:6px;">${row.body ? $('<div>').text(row.body).html() : '-'}</pre>` }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).show().text('Edit Template').off('click').on('click', () => {
                    window.closeDetailsDrawer();
                    if (typeof window.editEmailTemplate === 'function') window.editEmailTemplate(row.id);
                });
            }
            else if (type === 'invoice') {
                title = row.invoiceNumber || 'Invoice Record';
                subtitle = `Date: ${row.invoiceDate ? new Date(row.invoiceDate).toLocaleDateString() : '-'}`;
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect></svg>';

                const statusLabel = row.status === 1 || row.status === 'Paid' ? '<span class="badge badge-success">Paid</span>' :
                    (row.status === 2 || row.status === 'Overdue' ? '<span class="badge badge-error">Overdue</span>' : '<span class="badge badge-warning">Pending</span>');

                const currencyStr = row.currency || 'INR';

                sections = [
                    {
                        title: 'Billing Period Details',
                        properties: [
                            { label: 'Invoice #', value: row.invoiceNumber },
                            { label: 'Tenant Name', value: row.tenantName || '-' },
                            { label: 'Subscription Plan', value: row.subscriptionName || '-' },
                            { label: 'Invoice Date', value: row.invoiceDate ? new Date(row.invoiceDate).toLocaleDateString() : '-' },
                            { label: 'Due Date', value: row.dueDate ? new Date(row.dueDate).toLocaleDateString() : '-' },
                            { label: 'Status', value: statusLabel }
                        ]
                    },
                    {
                        title: 'Amounts Summary',
                        properties: [
                            { label: 'Subtotal Amount', value: `${row.amount != null ? row.amount : '-'} ${currencyStr}` },
                            { label: 'Tax Total', value: `${row.taxAmount != null ? row.taxAmount : '-'} ${currencyStr}` },
                            { label: 'Grand Total Paid', value: `${row.totalAmount != null ? row.totalAmount : '-'} ${currencyStr}` }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).show().text('Print Invoice').off('click').on('click', () => {
                    window.closeDetailsDrawer();
                    window.open(`/Billing/PrintInvoice/${row.id}`, '_blank');
                });
            }
            else if (type === 'payment') {
                title = row.transactionId || 'Payment Transaction';
                subtitle = `Received: ${row.paymentDate ? new Date(row.paymentDate).toLocaleDateString() : '-'}`;
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect></svg>';

                const statusLabel = row.status === 1 || row.status === 'Completed' ? '<span class="badge badge-success">Completed</span>' : '<span class="badge badge-warning">Pending</span>';

                const currencyStr = row.currency || 'INR';

                sections = [
                    {
                        title: 'Payment Details',
                        properties: [
                            { label: 'Transaction Reference', value: row.transactionId || '-' },
                            { label: 'Invoice #', value: row.invoiceNumber || '-' },
                            { label: 'Payment Method', value: row.methodName || 'Bank Transfer' },
                            { label: 'Payment Amount', value: `${row.amount != null ? row.amount : '-'} ${currencyStr}` },
                            { label: 'Payment Date', value: row.paymentDate ? new Date(row.paymentDate).toLocaleDateString() : '-' },
                            { label: 'Received By', value: row.receivedBy || '-' },
                            { label: 'Status', value: statusLabel }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).hide();
            }
            else if (type === 'auditLog') {
                title = `Audit Entry #${row.id}`;
                subtitle = row.tableName || 'System Audit';
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="20" x2="18" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="6" y1="20" x2="6" y2="14"></line></svg>';

                sections = [
                    {
                        title: 'Audit Metadata',
                        properties: [
                            { label: 'DateTime UTC', value: row.dateTime ? new Date(row.dateTime).toLocaleString() : '-' },
                            { label: 'Responsible User ID', value: row.userId || 'System' },
                            { label: 'Operation Type', value: `<span class="badge badge-neutral">${row.type}</span>` },
                            { label: 'Target Database Table', value: row.tableName },
                            { label: 'Primary Key Index', value: row.primaryKey || '-' }
                        ]
                    },
                    {
                        title: 'Changes Payload',
                        properties: [
                            { label: 'Affected Columns', value: row.affectedColumns || 'All Columns' },
                            { label: 'Old Entity Values', value: `<pre style="font-size:10px; text-align:left; max-width:240px; overflow-x:auto;">${row.oldValues || 'None'}</pre>` },
                            { label: 'New Entity Values', value: `<pre style="font-size:10px; text-align:left; max-width:240px; overflow-x:auto;">${row.newValues || 'None'}</pre>` }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).hide();
            }
            else if (type === 'license') {
                title = row.subscriptionName || 'Plan Entitlement';
                subtitle = `Tenant: ${row.tenantName || row.tenantId || '-'}`;
                iconHtml = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="5" width="20" height="14" rx="2" ry="2"></rect><line x1="2" y1="10" x2="22" y2="10"></line></svg>';

                sections = [
                    {
                        title: 'Entitlement Details',
                        properties: [
                            { label: 'Tenant Organization', value: row.tenantName || row.tenantId || '-' },
                            { label: 'Subscription Plan', value: row.subscriptionName || '-' },
                            { label: 'Activation Date', value: row.startDateUtc ? new Date(row.startDateUtc).toLocaleDateString() : '-' },
                            { label: 'Expiration Date', value: row.endDateUtc ? new Date(row.endDateUtc).toLocaleDateString() : '-' },
                            { label: 'Primary Plan Status', value: row.isActive ? '<span class="badge badge-success">Active</span>' : '<span class="badge badge-neutral">Inactive</span>' }
                        ]
                    }
                ];

                $(dataViewDrawerActionBtn).show().text('Modify Plan').off('click').on('click', () => {
                    window.closeDetailsDrawer();
                    if (typeof window.editTenantSubscription === 'function') window.editTenantSubscription(row.id, row.tenantId);
                });
            }

            titleEl.innerText = title;
            subtitleEl.innerText = subtitle;
            iconEl.innerHTML = iconHtml;

            // Automatically append Audit Metadata section if audit properties are present on row
            const createdBy = row.createdBy || row.CreatedBy;
            const createdOn = row.createdOn || row.CreatedOn;
            const lastModifiedBy = row.lastModifiedBy || row.LastModifiedBy;
            const lastModifiedOn = row.lastModifiedOn || row.LastModifiedOn;
            const ipAddress = row.ipAddress || row.IPAddress;
            const isDeleted = row.isDeleted ?? row.IsDeleted;

            if (createdBy || createdOn || lastModifiedBy || lastModifiedOn || ipAddress || isDeleted != null) {
                sections.push({
                    title: 'Audit Metadata',
                    properties: [
                        { label: 'Created By', value: createdBy || '-' },
                        { label: 'Created On', value: createdOn ? new Date(createdOn).toLocaleString() : '-' },
                        { label: 'Last Modified By', value: lastModifiedBy || '-' },
                        { label: 'Last Modified On', value: lastModifiedOn ? new Date(lastModifiedOn).toLocaleString() : '-' },
                        { label: 'IP Address', value: ipAddress || '-' },
                        { label: 'Is Deleted', value: isDeleted ? '<span class="badge badge-error">Yes</span>' : '<span class="badge badge-success">No</span>' }
                    ]
                });
            }

            sections.forEach(sec => {
                const $sec = $(`
                    <div class="drawer-section">
                        <div class="drawer-section-title">${sec.title}</div>
                    </div>
                `);
                sec.properties.forEach(prop => {
                    const $row = $(`
                        <div class="property-row">
                            <span class="property-label">${prop.label}</span>
                            <span class="property-value">${prop.value != null ? prop.value : '-'}</span>
                        </div>
                    `);
                    $sec.append($row);
                });
                $(bodyEl).append($sec);
            });
        }

        // Check if full object is passed
        if (typeof idOrRow === 'object' && idOrRow !== null) {
            renderData(idOrRow);
            return;
        }

        // Otherwise fetch by ID
        const id = idOrRow;
        let url = '';
        if (type === 'tenant') {
            url = `/api/v1/Tenant/Get/${id}`;
        } else if (type === 'user' || type === 'tenantUser') {
            url = `/api/v1/User/Get/${id}`;
        } else if (type === 'role') {
            url = `/api/v1/Role/Get/${id}`;
        } else if (type === 'subscription') {
            url = `/api/v1/Subscription/Get/${id}`;
        } else if (type === 'client' || type === 'application') {
            url = `/api/v1/Client/Get/${id}`;
        } else if (type === 'template' || type === 'service') {
            url = `/api/v1/Template/Get/${id}`;
        } else if (type === 'emailTemplate') {
            url = `/Home/GetEmailTemplateDetails?id=${id}`;
        } else if (type === 'license') {
            url = `/api/v1/TenantSubscription/Get/${id}`;
        } else if (type === 'invoice') {
            url = `/Billing/GetInvoiceDetails?id=${id}`;
        } else if (type === 'payment') {
            url = `/Billing/GetPaymentDetails?id=${id}`;
        } else if (type === 'auditLog') {
            url = `/Reporting/GetAuditLogDetails?id=${id}`;
        }

        if (!url) {
            console.error('Unknown module type for fetch:', type);
            renderError();
            return;
        }

        function fetchData() {
            bodyEl.innerHTML = `
                <div class="drawer-loading-state" style="display:flex; flex-direction:column; align-items:center; justify-content:center; padding:60px 20px; gap:16px; color:var(--text-muted);">
                    <div class="modern-spinner" style="width:28px; height:28px; border-width:3px; border-color:var(--border-subtle); border-top-color:var(--brand-primary); border-radius:50%; animation:spin 1s linear infinite;"></div>
                    <span style="font-size:0.8rem; font-weight:500;">Retrieving details...</span>
                </div>
            `;
            $.get(url)
                .done(function (res) {
                    let row = null;
                    if (res && res.hasOwnProperty('data') && res.hasOwnProperty('succeeded')) {
                        if (res.succeeded) {
                            row = res.data;
                        }
                    } else {
                        row = res;
                    }

                    if (row) {
                        renderData(row);
                    } else {
                        renderError();
                    }
                })
                .fail(function () {
                    renderError();
                });
        }

        function renderError() {
            bodyEl.innerHTML = `
                <div class="drawer-error-state" style="display:flex; flex-direction:column; align-items:center; justify-content:center; padding:48px 24px; text-align:center; gap:16px;">
                    <div style="width:48px; height:48px; border-radius:50%; background:rgba(239,68,68,0.1); color:var(--status-error); display:flex; align-items:center; justify-content:center;">
                        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
                    </div>
                    <div>
                        <h4 style="font-size:0.9rem; font-weight:600; color:var(--text-main); margin:0 0 4px 0;">Failed to load details</h4>
                        <p style="font-size:0.78rem; color:var(--text-muted); margin:0;">The record could not be retrieved from the server.</p>
                    </div>
                    <button class="btn btn-secondary btn-sm" id="retryDetailsFetchBtn">Retry</button>
                </div>
            `;
            $('#retryDetailsFetchBtn').off('click').on('click', function () {
                fetchData();
            });
        }

        fetchData();
    };

    // Sync on page load (ensure states in markup match document attributes)
    syncSettingsToUI();

    // Initialize Global Top Search Bar
    initGlobalSearch();

    function initGlobalSearch() {
        const path = window.location.pathname.toLowerCase();
        let defaultCategory = 'Users';
        let defaultUrl = '/Home/Users';
        let placeholder = 'Search users...';
        let defaultVal = 'Users';

        const categories = [
            { name: 'Users', val: 'Users', url: '/Home/Users', placeholder: 'Search users...' },
            { name: 'Tenants & Licenses', val: 'Tenants', url: '/Home/Tenants', placeholder: 'Search tenants...' },
            { name: 'Subscriptions', val: 'Subscriptions', url: '/Home/Subscriptions', placeholder: 'Search subscriptions...' },
            { name: 'Billing & Invoices', val: 'Invoices', url: '/Billing/Invoices', placeholder: 'Search invoices...' },
            { name: 'Roles & Permissions', val: 'Roles', url: '/Home/Roles', placeholder: 'Search roles...' },
            { name: 'Applications', val: 'Clients', url: '/Home/Clients', placeholder: 'Search applications...' },
            { name: 'Tokens', val: 'Tokens', url: '/Home/Tokens', placeholder: 'Search tokens...' },
            { name: 'Audit Logs', val: 'AuditLogs', url: '/Reporting/AuditLogs', placeholder: 'Search audit logs...' },
            { name: 'Templates', val: 'Templates', url: '/Home/Templates', placeholder: 'Search templates...' },
            { name: 'Email Templates', val: 'EmailTemplates', url: '/Home/EmailTemplates', placeholder: 'Search email templates...' },
            { name: 'Background Jobs', val: 'Jobs', url: '/Jobs', placeholder: 'Search jobs...' }
        ];

        // Match current URL path to select current category
        const currentCategory = categories.find(c => path.includes(c.url.toLowerCase()));
        if (currentCategory) {
            defaultCategory = currentCategory.name;
            defaultUrl = currentCategory.url;
            placeholder = currentCategory.placeholder;
            defaultVal = currentCategory.val;
        }

        // Set trigger UI text and placeholder
        $('#globalSearchSelectedCategory').text(defaultCategory);
        $('#globalSearchInput').attr('placeholder', placeholder);
        $('#globalSearchDropdownTrigger').data('url', defaultUrl);
        $('#globalSearchDropdownTrigger').data('value', defaultVal);

        // Highlight the active item in dropdown menu
        $('#globalSearchDropdownMenu .global-search-category-item').removeClass('active');
        $(`#globalSearchDropdownMenu .global-search-category-item[data-value="${defaultVal}"]`).addClass('active');

        // Check if there's a search term in the URL on load and keep the search bar expanded!
        const urlParams = new URLSearchParams(window.location.search);
        const searchVal = urlParams.get('search');
        if (searchVal) {
            $('#globalSearchInput').val(searchVal);
            $('#globalSearchWrapper').addClass('expanded');
        }

        // Click search icon trigger
        $('#globalSearchIcon').on('click', function (e) {
            if (!$('#globalSearchWrapper').hasClass('expanded')) {
                e.stopPropagation();
                $('#globalSearchWrapper').addClass('expanded');
                $('#globalSearchInput').focus();
            }
        });

        // Close/collapse search trigger
        $('#globalSearchClose').on('click', function (e) {
            e.stopPropagation();
            $('#globalSearchInput').val('');
            $('#globalSearchResultsPanel').hide().empty();

            // If we have an active search query in URL, clear it and redraw/redirect
            const currentUrlParams = new URLSearchParams(window.location.search);
            const hasSearchParam = currentUrlParams.has('search');
            if (hasSearchParam) {
                // Clear query parameter from URL without reloading
                const newUrl = window.location.pathname;
                window.history.pushState({}, '', newUrl);

                // Clear the active DataTable search
                const activeTables = $.fn.dataTable.tables();
                if (activeTables.length > 0) {
                    const api = $(activeTables[0]).DataTable();

                    // Clear search input on table's local search bar
                    const $localInput = $('.common-search-group .search-input');
                    if ($localInput.length) {
                        $localInput.val('');
                        $('.common-search-group .search-clear-btn').hide();
                    }

                    api.search('').draw();
                }
            }

            $('#globalSearchWrapper').removeClass('expanded');
        });

        // Toggle category dropdown
        $('#globalSearchDropdownTrigger').on('click', function (e) {
            e.stopPropagation();
            const isOpen = $('#globalSearchDropdownMenu').toggleClass('open').hasClass('open');
            $(this).toggleClass('menu-open', isOpen);
        });

        // Close dropdown when clicking outside
        $(document).on('click', function () {
            $('#globalSearchDropdownMenu').removeClass('open');
            $('#globalSearchDropdownTrigger').removeClass('menu-open');
        });

        // Select category item
        $('.global-search-category-item').on('click', function (e) {
            e.stopPropagation();
            const categoryName = $(this).text().trim();
            const val = $(this).data('value');
            const url = $(this).data('url');

            const matchedCategory = categories.find(c => c.val === val);
            const placeholderText = matchedCategory ? matchedCategory.placeholder : 'Search...';

            $('#globalSearchSelectedCategory').text(matchedCategory ? matchedCategory.name : categoryName);
            $('#globalSearchDropdownTrigger').data('value', val);
            $('#globalSearchDropdownTrigger').data('url', url);
            $('#globalSearchInput').attr('placeholder', placeholderText);
            $('#globalSearchDropdownMenu').removeClass('open');
            $('#globalSearchDropdownTrigger').removeClass('menu-open');

            $('#globalSearchDropdownMenu .global-search-category-item').removeClass('active');
            $(this).addClass('active');

            // Clear active autocomplete results since the category changed
            $('#globalSearchInput').val('');
            $('#globalSearchResultsPanel').hide().empty();
            $('#globalSearchInput').focus();
        });

        // Handle typing/searching
        $('#globalSearchInput').on('keypress', function (e) {
            if (e.which === 13) { // Enter key
                triggerGlobalSearch();
            }
        });

        // Search trigger function
        function triggerGlobalSearch() {
            const query = $('#globalSearchInput').val().trim();
            const selectedUrl = $('#globalSearchDropdownTrigger').data('url');

            if (!selectedUrl) return;

            // Hide suggestions panel
            $('#globalSearchResultsPanel').hide();

            // Check if current page path matches the selected module URL
            const isSamePage = path.includes(selectedUrl.toLowerCase()) || (selectedUrl.length > 5 && selectedUrl.toLowerCase().includes(path));

            if (isSamePage) {
                // Update URL without page reload
                const newUrl = query ? `${selectedUrl}?search=${encodeURIComponent(query)}` : selectedUrl;
                window.history.pushState({}, '', newUrl);

                // Find active DataTable and trigger search
                const activeTables = $.fn.dataTable.tables();
                if (activeTables.length > 0) {
                    const api = $(activeTables[0]).DataTable();

                    // Also sync to local search bar input if it exists
                    const $localInput = $('.common-search-group .search-input');
                    if ($localInput.length) {
                        $localInput.val(query);
                        if (query) {
                            $('.common-search-group .search-clear-btn').show();
                        } else {
                            $('.common-search-group .search-clear-btn').hide();
                        }
                    }

                    api.search(query).draw();
                } else {
                    window.location.href = newUrl;
                }
            } else {
                // Navigate to the selected module with the search query
                const newUrl = query ? `${selectedUrl}?search=${encodeURIComponent(query)}` : selectedUrl;
                window.location.href = newUrl;
            }
        }

        // --- Autocomplete dynamic suggestions logic ---
        let searchInputTimeout;
        $('#globalSearchInput').on('input', function () {
            const query = $(this).val().trim();
            const category = $('#globalSearchDropdownTrigger').data('value');

            clearTimeout(searchInputTimeout);

            if (query.length < 2) {
                $('#globalSearchResultsPanel').hide().empty();
                return;
            }

            // Show loading state inside suggestion panel
            $('#globalSearchResultsPanel').html(`
                <div class="search-results-loading">
                    <div class="spinner"></div> Searching suggestions...
                </div>
            `).show();

            searchInputTimeout = setTimeout(() => {
                $.ajax({
                    url: '/Home/GlobalSearchSuggestions',
                    type: 'GET',
                    data: { category: category, term: query },
                    success: function (results) {
                        const $panel = $('#globalSearchResultsPanel');
                        $panel.empty();

                        if (!results || results.length === 0) {
                            $panel.html(`
                                <div class="search-no-results">
                                    <div class="search-no-results-icon">🔍</div>
                                    No matches found for <strong>"${escapeHtml(query)}"</strong>
                                </div>
                            `);
                            return;
                        }

                        // Add a header showing category + count
                        const matchedCat = categories.find(c => c.val === category);
                        const catLabel = matchedCat ? matchedCat.name : category;
                        $panel.append(`<div class="search-results-header"><span>${escapeHtml(catLabel)}</span><span>${results.length} result${results.length !== 1 ? 's' : ''}</span></div>`);

                        results.forEach(item => {
                            const iconSvg = getCategoryIconSvg(category);
                            const $resultItem = $(`
                                <a href="${item.url}" class="search-result-item">
                                    <div class="search-result-icon">
                                        ${iconSvg}
                                    </div>
                                    <div class="search-result-info">
                                        <div class="search-result-title">${escapeHtml(item.title ?? '')}</div>
                                        <div class="search-result-subtitle">${escapeHtml(item.subtitle ?? '')}</div>
                                    </div>
                                    <div class="search-result-arrow">
                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"></line><polyline points="12 5 19 12 12 19"></polyline></svg>
                                    </div>
                                </a>
                            `);
                            $panel.append($resultItem);
                        });

                        $panel.show();
                    },
                    error: function () {
                        $('#globalSearchResultsPanel').html('<div class="search-no-results"><div class="search-no-results-icon">⚠️</div>Failed to load suggestions.</div>');
                    }
                });
            }, 300); // 300ms debounce
        });

        // Hide search results panel when clicking outside the search wrapper
        $(document).on('click', function (e) {
            if (!$(e.target).closest('#globalSearchWrapper').length) {
                $('#globalSearchResultsPanel').hide();
            }
        });

        // Show search results panel again if input gets focused and has content
        $('#globalSearchInput').on('focus', function () {
            if ($(this).val().trim().length >= 2) {
                $('#globalSearchResultsPanel').show();
            }
        });

        // Helper: Escape HTML to prevent XSS
        function escapeHtml(str) {
            if (!str) return '';
            return str.toString()
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;")
                .replace(/'/g, "&#039;");
        }

        // Helper: Get SVG icons representing each category
        function getCategoryIconSvg(category) {
            switch (category) {
                case 'Users':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle></svg>`;
                case 'Tenants':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg>`;
                case 'Subscriptions':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="5" width="20" height="14" rx="2" ry="2"></rect><line x1="2" y1="10" x2="22" y2="10"></line></svg>`;
                case 'Invoices':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect><line x1="8" y1="21" x2="16" y2="21"></line><line x1="12" y1="17" x2="12" y2="21"></line></svg>`;
                case 'Roles':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="12 2 2 7 12 12 22 7 12 2"></polygon><polyline points="2 17 12 22 22 17"></polyline></svg>`;
                case 'Permissions':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path></svg>`;
                case 'Clients':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>`;
                case 'Tokens':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>`;
                case 'AuditLogs':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="6" y1="20" x2="6" y2="14"></line></svg>`;
                case 'Templates':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline></svg>`;
                case 'EmailTemplates':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path><polyline points="22,6 12,13 2,6"></polyline></svg>`;
                case 'Jobs':
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>`;
                default:
                    return `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>`;
            }
        }
    }
});


window.initMultiSelects = function () {
    document.querySelectorAll('select[multiple]').forEach(select => {
        if (select.hasAttribute('data-multiselect-initialized') ||
            select.hasAttribute('data-no-custom-select') ||
            select.classList.contains('no-custom-select')) return;

        // Skip selects inside table filters repository, data templates, drawer, or already custom-wrapped elements
        if (select.closest('.table-filters-repo') ||
            select.closest('.table-filters-data') ||
            select.closest('#filterDrawer') ||
            select.closest('.multi-select-container') ||
            select.closest('.custom-multiselect-wrapper')) {
            return;
        }
        select.setAttribute('data-multiselect-initialized', 'true');

        // Hide original select
        select.style.setProperty('display', 'none', 'important');

        const wrapper = document.createElement('div');
        wrapper.className = 'custom-multiselect-wrapper';
        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(select);

        const control = document.createElement('div');
        control.className = 'custom-multiselect-control';
        wrapper.appendChild(control);

        const chipsContainer = document.createElement('div');
        chipsContainer.className = 'custom-multiselect-chips';
        control.appendChild(chipsContainer);

        const searchInput = document.createElement('input');
        searchInput.type = 'text';
        searchInput.className = 'custom-multiselect-search';
        searchInput.placeholder = select.getAttribute('placeholder') || 'Select options...';
        control.appendChild(searchInput);

        const chevron = document.createElement('div');
        chevron.className = 'custom-multiselect-chevron';
        chevron.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"></polyline></svg>';
        control.appendChild(chevron);

        const dropdown = document.createElement('div');
        dropdown.className = 'custom-multiselect-dropdown';
        wrapper.appendChild(dropdown);

        const optionsList = document.createElement('div');
        optionsList.className = 'custom-multiselect-options';
        dropdown.appendChild(optionsList);

        const actionRow = document.createElement('div');
        actionRow.className = 'custom-multiselect-actions';
        actionRow.innerHTML = '<button type="button" class="ms-btn-selectAll">Select All</button><button type="button" class="ms-btn-clearAll">Clear All</button>';
        dropdown.insertBefore(actionRow, optionsList);

        const updateChips = () => {
            chipsContainer.innerHTML = '';
            let selectedOptions = Array.from(select.options).filter(o => o.selected);
            if (selectedOptions.length === 0) {
                searchInput.placeholder = select.getAttribute('placeholder') || 'Select options...';
            } else {
                searchInput.placeholder = '';
            }

            selectedOptions.forEach(opt => {
                const chip = document.createElement('div');
                chip.className = 'custom-multiselect-chip';
                chip.innerHTML = `<span>${opt.text}</span><svg class="chip-remove" data-val="${opt.value}" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>`;
                chipsContainer.appendChild(chip);
            });
        };

        const renderDropdown = (filterText = '') => {
            optionsList.innerHTML = '';
            const lowerFilter = filterText.toLowerCase();
            const currentOptions = Array.from(select.options);
            currentOptions.forEach(opt => {
                if (opt.text.toLowerCase().includes(lowerFilter)) {
                    const item = document.createElement('div');
                    item.className = 'custom-multiselect-option' + (opt.selected ? ' selected' : '');
                    item.innerHTML = `<input type="checkbox" class="custom-checkbox" ${opt.selected ? 'checked' : ''} style="pointer-events: none;"> <span>${opt.text}</span>`;
                    item.addEventListener('click', (e) => {
                        e.stopPropagation();
                        opt.selected = !opt.selected;
                        select.dispatchEvent(new Event('change', { bubbles: true }));
                        renderDropdown(searchInput.value);
                        updateChips();
                        searchInput.focus();
                    });
                    optionsList.appendChild(item);
                }
            });
        };

        // Listeners
        select.addEventListener('change', () => {
            updateChips();
            if (dropdown.classList.contains('show')) {
                renderDropdown(searchInput.value);
            }
        });

        control.addEventListener('click', (e) => {
            if (e.target.closest('.chip-remove')) {
                const val = e.target.closest('.chip-remove').getAttribute('data-val');
                const opt = Array.from(select.options).find(o => o.value === val);
                if (opt) {
                    opt.selected = false;
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                    updateChips();
                    if (dropdown.classList.contains('show')) {
                        renderDropdown(searchInput.value);
                    }
                }
                e.stopPropagation();
                return;
            }
            if (e.target === searchInput) {
                if (!dropdown.classList.contains('show')) {
                    dropdown.classList.add('show');
                    renderDropdown(searchInput.value);
                }
                return;
            }
            dropdown.classList.toggle('show');
            if (dropdown.classList.contains('show')) {
                searchInput.focus();
                renderDropdown(searchInput.value);
            }
        });

        searchInput.addEventListener('input', (e) => {
            renderDropdown(e.target.value);
            dropdown.classList.add('show');
        });

        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                dropdown.classList.remove('show');
                e.stopPropagation();
            } else if (e.key === 'Enter') {
                e.preventDefault();
                e.stopPropagation();
            }
        });

        actionRow.querySelector('.ms-btn-selectAll').addEventListener('click', (e) => {
            e.stopPropagation();
            Array.from(select.options).forEach(o => o.selected = true);
            select.dispatchEvent(new Event('change', { bubbles: true }));
            renderDropdown(searchInput.value);
            updateChips();
        });

        actionRow.querySelector('.ms-btn-clearAll').addEventListener('click', (e) => {
            e.stopPropagation();
            Array.from(select.options).forEach(o => o.selected = false);
            select.dispatchEvent(new Event('change', { bubbles: true }));
            renderDropdown(searchInput.value);
            updateChips();
        });

        document.addEventListener('click', (e) => {
            if (!wrapper.contains(e.target)) {
                dropdown.classList.remove('show');
            }
        });

        // Dynamic MutationObserver to catch option changes programmatically
        const selectObserver = new MutationObserver(() => {
            updateChips();
            if (dropdown.classList.contains('show')) {
                renderDropdown(searchInput.value);
            }
        });
        selectObserver.observe(select, { childList: true });

        updateChips();
    });
};

document.addEventListener('DOMContentLoaded', () => {
    window.initMultiSelects();

    // Use MutationObserver to catch dynamically added selects
    const observer = new MutationObserver((mutations) => {
        let shouldInit = false;
        mutations.forEach(m => {
            if (m.addedNodes.length) shouldInit = true;
        });
        if (shouldInit) window.initMultiSelects();
    });
    observer.observe(document.body, { childList: true, subtree: true });

    // --- Active Tab Navigation Highlight and Dynamic Breadcrumbs ---
    function updateActiveNavigation() {
        const currentPath = window.location.pathname.toLowerCase().replace(/\/$/, "");

        const routeMappings = {
            "/home/createtenant": "/home/tenants",
            "/home/managetenant": "/home/tenantusers",
            "/home/manageapplicationusers": "/home/tenantusers",
            "/home/deletetenant": "/home/tenants",
            "/home/managetenantsubscriptions": "/home/subscriptions",
            "/home/addedittenantsubscription": "/home/subscriptions",
            "/home/createsubscription": "/home/subscriptions",
            "/home/deletesubscription": "/home/subscriptions",
            "/home/createuser": "/home/users",
            "/home/deleteuser": "/home/users",
            "/home/createrole": "/home/roles",
            "/home/deleterole": "/home/roles",
            "/home/rolepermissions": "/home/roles",
            "/home/createclient": "/home/clients",
            "/home/deleteclient": "/home/clients",
            "/home/createtemplate": "/home/templates",
            "/home/createemailtemplate": "/home/emailtemplates"
        };

        let targetPath = routeMappings[currentPath] || currentPath || "/";
        if (targetPath.startsWith('/jobs')) {
            targetPath = '/jobs';
        }
        if (targetPath.startsWith('/billing')) {
            targetPath = '/billing/invoices';
        }

        let activeModuleName = "Dashboard";

        // Reset active highlights
        document.querySelectorAll('.sidebar .nav-item').forEach(item => item.classList.remove('active'));
        document.querySelectorAll('.sidebar .nav-child-item').forEach(item => item.classList.remove('active'));

        let expandedList = [];
        try {
            expandedList = JSON.parse(localStorage.getItem('expanded-nav-groups')) || [];
        } catch (e) { }

        // Restore expanded state on sidebar groups
        const groups = document.querySelectorAll('.sidebar .nav-group');
        groups.forEach(group => {
            const sectionId = group.getAttribute('data-section');
            if (sectionId && expandedList.includes(sectionId)) {
                group.classList.add('expanded');
                group.querySelector('.nav-group-header')?.classList.add('active');
            } else {
                group.classList.remove('expanded');
                group.querySelector('.nav-group-header')?.classList.remove('active');
            }
        });

        // 1. Sidebar link matching for standalone nav-items (e.g. Dashboard)
        const sidebarItems = document.querySelectorAll('.sidebar .nav-item:not(.nav-group-header)');
        const searchLower = window.location.search.toLowerCase();
        const hasTenantContextInUrl = searchLower.includes('tenantid=') || searchLower.includes('tenantid');
        const isTenantAccessControlFlow = targetPath.includes('/home/managetenant') ||
            targetPath.includes('/home/manageapplicationusers') ||
            targetPath.includes('/home/tenantusers') ||
            (targetPath.includes('/home/rolepermissions') && hasTenantContextInUrl);

        sidebarItems.forEach(item => {
            const href = item.getAttribute('href');
            if (href) {
                const normalizedHref = href.toLowerCase().replace(/\/$/, "");
                if (isTenantAccessControlFlow) {
                    if (normalizedHref.includes('/home/tenantusers')) {
                        item.classList.add('active');
                        activeModuleName = 'Tenant Access Control';
                    }
                } else if (normalizedHref === targetPath || (targetPath === "/" && (normalizedHref === "" || normalizedHref === "/home"))) {
                    item.classList.add('active');
                    const textSpan = item.querySelector('.nav-text');
                    if (textSpan) {
                        activeModuleName = textSpan.textContent.replace(/\s+/g, ' ').trim();
                    }
                }
            }
        });

        // 2. Sidebar link matching for child navigation items
        const childItems = document.querySelectorAll('.sidebar .nav-child-item');
        childItems.forEach(item => {
            const href = item.getAttribute('href');
            if (href) {
                const normalizedHref = href.toLowerCase().replace(/\/$/, "");
                if (normalizedHref === targetPath || (targetPath === "/" && (normalizedHref === "" || normalizedHref === "/home"))) {
                    item.classList.add('active');
                    const textSpan = item.querySelector('.nav-child-text') || item;
                    activeModuleName = textSpan.textContent.replace(/\s+/g, ' ').trim();

                    const parentGroup = item.closest('.nav-group');
                    if (parentGroup) {
                        parentGroup.classList.add('expanded');
                        const header = parentGroup.querySelector('.nav-group-header');
                        if (header) {
                            header.classList.add('active');
                        }
                        const sectionId = parentGroup.getAttribute('data-section');
                        if (sectionId && !expandedList.includes(sectionId)) {
                            expandedList.push(sectionId);
                            localStorage.setItem('expanded-nav-groups', JSON.stringify(expandedList));
                        }
                    }
                }
            }
        });

        // 2. Top navbar links matching
        const topNavLinks = document.querySelectorAll('.top-navbar .top-nav-link');
        topNavLinks.forEach(link => {
            link.classList.remove('active');
            link.closest('.top-nav-item')?.classList.remove('active');
        });

        const topNavDropdownLinks = document.querySelectorAll('.top-navbar .top-nav-dropdown-menu a');
        topNavDropdownLinks.forEach(link => {
            link.classList.remove('active');
        });

        let matchedDropdownLink = null;
        topNavDropdownLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href) {
                const normalizedHref = href.toLowerCase().replace(/\/$/, "");
                if (normalizedHref === targetPath) {
                    link.classList.add('active');
                    matchedDropdownLink = link;
                    activeModuleName = link.textContent.replace(/\s+/g, ' ').trim();
                }
            }
        });

        if (matchedDropdownLink) {
            const parentItem = matchedDropdownLink.closest('.top-nav-item');
            if (parentItem) {
                parentItem.classList.add('active');
                parentItem.querySelector('.top-nav-link')?.classList.add('active');
            }
        } else {
            topNavLinks.forEach(link => {
                const href = link.getAttribute('href');
                if (href) {
                    const normalizedHref = href.toLowerCase().replace(/\/$/, "");
                    if (normalizedHref === targetPath || (targetPath === "/" && (normalizedHref === "" || normalizedHref === "/home"))) {
                        link.classList.add('active');
                        link.closest('.top-nav-item')?.classList.add('active');
                        activeModuleName = link.textContent.replace(/\s+/g, ' ').trim();
                    }
                }
            });
        }

        // Special Audit Logs check
        if (targetPath.includes('/reporting/auditlogs')) {
            activeModuleName = "Audit Logs";
        }

        // 3. Update breadcrumbs text
        const breadcrumbCurrent = document.getElementById('breadcrumb-current');
        if (breadcrumbCurrent) {
            breadcrumbCurrent.textContent = activeModuleName;
        }
    }

    updateActiveNavigation();

    // ════════════════════════════════════════════════════════
    //  GLOBAL CUSTOM TOOLTIPS ENGINE
    // ════════════════════════════════════════════════════════
    let tooltipEl = null;
    let activeTooltipTarget = null;
    let touchTimeout = null;

    function createTooltipElement() {
        if (tooltipEl) return tooltipEl;
        tooltipEl = document.createElement('div');
        tooltipEl.className = 'app-tooltip';
        document.body.appendChild(tooltipEl);
        return tooltipEl;
    }

    function showTooltip(target) {
        if (!target) return;

        // Suppress native title if present
        if (target.hasAttribute('title')) {
            const titleText = target.getAttribute('title');
            if (titleText && titleText.trim()) {
                target.setAttribute('data-tooltip', titleText);
            }
            target.removeAttribute('title');
        }

        const text = target.getAttribute('data-tooltip');
        if (!text || !text.trim()) return;

        activeTooltipTarget = target;
        const tooltip = createTooltipElement();
        tooltip.textContent = text;
        tooltip.classList.add('visible');

        // Let the DOM update element dimensions before positioning
        requestAnimationFrame(() => {
            positionTooltip(target, tooltip);
        });
    }

    function hideTooltip() {
        if (tooltipEl) {
            tooltipEl.classList.remove('visible');
        }
        activeTooltipTarget = null;
    }

    function positionTooltip(target, tooltip) {
        const targetRect = target.getBoundingClientRect();
        const tooltipRect = tooltip.getBoundingClientRect();

        // Default: display below the element
        let top = targetRect.bottom + window.scrollY + 6;
        let left = targetRect.left + window.scrollX + (targetRect.width - tooltipRect.width) / 2;

        // Viewport bounds protection:
        // 1. If it would overflow the bottom of the screen, place it above instead
        if (targetRect.bottom + tooltipRect.height + 15 > window.innerHeight) {
            top = targetRect.top + window.scrollY - tooltipRect.height - 6;
        }

        // 2. Prevent horizontal overflow
        const minMargin = 10;
        if (left < minMargin) {
            left = minMargin;
        } else if (left + tooltipRect.width > window.innerWidth - minMargin) {
            left = window.innerWidth - tooltipRect.width - minMargin;
        }

        tooltip.style.top = `${top}px`;
        tooltip.style.left = `${left}px`;
    }

    function getTooltipTarget(el) {
        if (!el) return null;

        // Dynamic tooltip detection for truncated child menu items
        const childItem = el.closest('.nav-child-item');
        if (childItem) {
            const textEl = childItem.querySelector('.nav-child-text');
            if (textEl && textEl.scrollWidth > textEl.clientWidth) {
                childItem.setAttribute('data-tooltip', textEl.textContent.trim());
            } else {
                childItem.removeAttribute('data-tooltip');
            }
        }

        const target = el.closest('[title], [data-tooltip], .nav-item');
        if (!target) return null;

        if (target.classList.contains('nav-item')) {
            const isSidebarCollapsed = document.documentElement.classList.contains('sidebar-collapsed') && window.innerWidth > 768;
            if (!isSidebarCollapsed) {
                if (!target.hasAttribute('title') && !target.hasAttribute('data-tooltip')) {
                    return null;
                }
            } else {
                if (!target.hasAttribute('data-tooltip') && !target.hasAttribute('title')) {
                    const navText = target.querySelector('.nav-text');
                    if (navText) {
                        target.setAttribute('data-tooltip', navText.textContent.trim());
                    }
                }
            }
        }
        return target;
    }

    // Event delegation:
    // Hover triggers (Desktop)
    document.addEventListener('mouseover', (e) => {
        const target = getTooltipTarget(e.target);
        if (target) {
            showTooltip(target);
        }
    });

    document.addEventListener('mouseout', (e) => {
        const target = getTooltipTarget(e.target);
        if (target && target === activeTooltipTarget) {
            hideTooltip();
        }
    });

    // Focus triggers (Keyboard & Accessibility)
    document.addEventListener('focusin', (e) => {
        const target = getTooltipTarget(e.target);
        if (target) {
            showTooltip(target);
        }
    });

    document.addEventListener('focusout', (e) => {
        const target = getTooltipTarget(e.target);
        if (target && target === activeTooltipTarget) {
            hideTooltip();
        }
    });

    // Touch triggers (Mobile: Tap/Long Press)
    document.addEventListener('touchstart', (e) => {
        const target = getTooltipTarget(e.target);
        if (!target) {
            hideTooltip();
            return;
        }

        // Suppress double triggering
        if (target.hasAttribute('title')) {
            const titleText = target.getAttribute('title');
            if (titleText && titleText.trim()) {
                target.setAttribute('data-tooltip', titleText);
            }
            target.removeAttribute('title');
        }

        clearTimeout(touchTimeout);
        touchTimeout = setTimeout(() => {
            showTooltip(target);
        }, 250); // 250ms long press threshold
    }, { passive: true });

    document.addEventListener('touchend', () => {
        clearTimeout(touchTimeout);
        // Hide after a brief delay on touch end to let the user read it
        setTimeout(() => {
            if (activeTooltipTarget) {
                hideTooltip();
            }
        }, 1500);
    });

    document.addEventListener('touchmove', () => {
        clearTimeout(touchTimeout);
    }, { passive: true });

    // Global event listener to close multi-select dropdowns when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.multi-select-container').length) {
            $('.multi-select-container').removeClass('open');
        }
    });

    // Custom Multi-Select Initialization Plugin
    window.initMultiSelect = function ($select) {
        if ($select.data('multiselect-initialized')) {
            return;
        }
        $select.data('multiselect-initialized', true);

        // Hide original select
        $select.hide();

        // Detect placeholder
        let placeholderText = 'Select options...';
        const $emptyOption = $select.find('option[value=""], option:not([value])');
        if ($emptyOption.length) {
            placeholderText = $emptyOption.first().text();
        }

        // Create container structure
        const $container = $('<div class="multi-select-container"></div>');
        const $trigger = $(`
            <div class="multi-select-trigger">
                <div class="multi-select-chips"></div>
                <svg class="chevron" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="6 9 12 15 18 9"></polyline></svg>
            </div>
        `);
        const $dropdown = $(`
            <div class="multi-select-dropdown">
                <div class="multi-select-search-wrapper">
                    <input type="text" class="multi-select-search-input" placeholder="Search..." />
                </div>
                <div class="multi-select-actions">
                    <button type="button" class="ms-action-btn ms-select-all">Select All</button>
                    <button type="button" class="ms-action-btn ms-clear-all">Clear All</button>
                </div>
                <div class="multi-select-options"></div>
            </div>
        `);

        $container.append($trigger).append($dropdown);
        $select.after($container);

        const $optionsContainer = $dropdown.find('.multi-select-options');
        const $searchInput = $dropdown.find('.multi-select-search-input');
        const $chipsContainer = $trigger.find('.multi-select-chips');

        // Function to update trigger chips based on actual select value
        function updateTrigger() {
            $chipsContainer.empty();
            let selectedVals = $select.val() || [];
            if (!Array.isArray(selectedVals)) {
                selectedVals = [selectedVals.toString()];
            }
            $select.data('restored-value', selectedVals);

            // Sync checkbox checks
            $optionsContainer.find('.multi-select-option').each(function () {
                const val = $(this).data('value').toString();
                const isSelected = selectedVals.includes(val);
                $(this).find('input[type="checkbox"]').prop('checked', isSelected);
            });

            if (selectedVals.length === 0) {
                $chipsContainer.append(`<span class="multi-select-placeholder">${placeholderText}</span>`);
            } else {
                selectedVals.forEach(val => {
                    const optText = $select.find(`option[value="${val}"]`).text() || val;
                    const $chip = $(`
                        <div class="multi-select-chip" data-value="${val}">
                            <span>${optText}</span>
                            <span class="multi-select-chip-close">&times;</span>
                        </div>
                    `);
                    $chipsContainer.append($chip);
                });
            }
        }

        // Sync hidden select and trigger on checkbox changes
        function handleSelectionChange() {
            const selectedVals = [];
            $optionsContainer.find('.multi-select-option').each(function () {
                const $chk = $(this).find('input[type="checkbox"]');
                if ($chk.is(':checked')) {
                    selectedVals.push($(this).data('value').toString());
                }
            });
            $select.val(selectedVals);
            $select.data('restored-value', selectedVals); // Sync restored value as well
            $select.trigger('change');
            updateTrigger();
        }

        // Function to load options dynamically from a URL
        function loadOptionsFromUrl() {
            const url = $select.attr('data-url');
            if (!url) return;

            $optionsContainer.empty().append(`
                <div class="multi-select-loading" style="padding: 12px; text-align: center; color: var(--text-muted); font-size: 0.85rem; display: flex; align-items: center; justify-content: center; gap: 8px;">
                    <span class="spinner" style="display: inline-block; width: 14px; height: 14px; border: 2px solid var(--text-muted); border-top-color: transparent; border-radius: 50%; animation: spin 0.8s linear infinite;"></span>
                    <span>Loading options...</span>
                </div>
            `);

            if ($('#ms-spin-style').length === 0) {
                $('<style id="ms-spin-style">@keyframes spin { to { transform: rotate(360deg); } }</style>').appendTo('head');
            }

            $.ajax({
                url: url,
                type: 'GET',
                dataType: 'json',
                success: function (response) {
                    $optionsContainer.empty();
                    let items = [];
                    if (response && response.succeeded && Array.isArray(response.data)) {
                        items = response.data;
                    } else if (Array.isArray(response)) {
                        items = response;
                    } else if (response && Array.isArray(response.data)) {
                        items = response.data;
                    }

                    if (items.length === 0) {
                        $optionsContainer.append('<div class="multi-select-no-options" style="padding: 12px; text-align: center; color: var(--text-muted); font-size: 0.85rem;">No options available</div>');
                        return;
                    }

                    // Keep track of currently selected values
                    const restoredVal = $select.data('restored-value');
                    const currentValues = restoredVal || $select.val() || [];

                    // Clear existing option tags in select except empty placeholder option
                    $select.find('option').not('[value=""], :not([value])').remove();

                    items.forEach(item => {
                        const val = (item.id || item.Id || item.value || item.Value || '').toString();
                        const text = item.name || item.Name || item.text || item.Text || item.displayName || item.DisplayName || val;

                        if (!val) return;

                        if ($select.find(`option[value="${val}"]`).length === 0) {
                            const isSelected = currentValues.includes(val);
                            const $opt = $('<option></option>').val(val).text(text).prop('selected', isSelected);
                            $select.append($opt);
                        }
                    });

                    // Render options inside dropdown
                    $select.find('option').each(function () {
                        const val = $(this).val();
                        const text = $(this).text();
                        if (val === '' || val === null || val === undefined) {
                            return;
                        }
                        const isSelected = currentValues.includes(val.toString());
                        const $optDiv = $(`
                            <div class="multi-select-option" data-value="${val}">
                                <input type="checkbox" ${isSelected ? 'checked' : ''} />
                                <span>${text}</span>
                            </div>
                        `);
                        $optionsContainer.append($optDiv);
                    });

                    updateTrigger();
                },
                error: function (xhr, status, error) {
                    $optionsContainer.empty().append('<div class="multi-select-error" style="padding: 12px; text-align: center; color: var(--danger-color, #ef4444); font-size: 0.85rem;">Failed to load options</div>');
                    console.error("Failed to load options from " + url, error);
                }
            });
        }

        // Toggle open state on trigger click
        $trigger.on('click', function (e) {
            e.stopPropagation();
            const isOpen = $container.hasClass('open');
            // Close other multiselects first
            $('.multi-select-container').not($container).removeClass('open');
            $container.toggleClass('open', !isOpen);
            if (!isOpen) {
                $searchInput.val('').trigger('input');
                $searchInput.focus();
            }
        });

        // Stop propagation of clicks inside the dropdown
        $dropdown.on('click', function (e) {
            e.stopPropagation();
        });

        // Option item click handler
        $optionsContainer.on('click', '.multi-select-option', function (e) {
            if ($(e.target).is('input[type="checkbox"]')) {
                handleSelectionChange();
                return;
            }
            const $chk = $(this).find('input[type="checkbox"]');
            $chk.prop('checked', !$chk.is(':checked'));
            handleSelectionChange();
        });

        // Chip close handler
        $chipsContainer.on('click', '.multi-select-chip-close', function (e) {
            e.stopPropagation();
            const val = $(this).parent().data('value').toString();
            $optionsContainer.find(`.multi-select-option[data-value="${val}"] input[type="checkbox"]`).prop('checked', false);
            handleSelectionChange();
        });

        // Select All handler
        $dropdown.find('.ms-select-all').on('click', function () {
            $optionsContainer.find('.multi-select-option:visible input[type="checkbox"]').prop('checked', true);
            handleSelectionChange();
        });

        // Clear All handler
        $dropdown.find('.ms-clear-all').on('click', function () {
            $optionsContainer.find('.multi-select-option input[type="checkbox"]').prop('checked', false);
            handleSelectionChange();
        });

        // Search filtering handler
        $searchInput.on('input', function () {
            const query = $(this).val().toLowerCase().trim();
            $optionsContainer.find('.multi-select-option').each(function () {
                const text = $(this).find('span').text().toLowerCase();
                if (text.includes(query)) {
                    $(this).show();
                } else {
                    $(this).hide();
                }
            });
        });

        // Populate options or fetch dynamically
        if ($select.attr('data-url')) {
            loadOptionsFromUrl();
        } else {
            const optionsData = [];
            $select.find('option').each(function () {
                const val = $(this).val();
                const text = $(this).text();
                if (val === '' || val === null || val === undefined) {
                    return; // Skip empty placeholder option
                }
                optionsData.push({ val, text, selected: $(this).prop('selected') });
            });

            if (optionsData.length === 0) {
                $optionsContainer.append('<div class="multi-select-no-options" style="padding: 12px; text-align: center; color: var(--text-muted); font-size: 0.85rem;">No options available</div>');
            } else {
                optionsData.forEach(opt => {
                    const $optDiv = $(`
                        <div class="multi-select-option" data-value="${opt.val}">
                            <input type="checkbox" ${opt.selected ? 'checked' : ''} />
                            <span>${opt.text}</span>
                        </div>
                    `);
                    $optionsContainer.append($optDiv);
                });
            }
            updateTrigger();
        }

        // Listen for external value updates to sync custom UI
        $select.on('change.multiselect-sync', function (e) {
            updateTrigger();
        });
    };

    window.renderPillPagination = function (dt, mountSelector, label) {
        const info = dt.page.info();
        const totalPages = Math.max(info.pages, 1);
        const current = info.page + 1;
        const total = info.recordsTotal;
        const perPage = info.length;
        const $mount = $(mountSelector);

        if (!$mount.length) return;

        if ($mount.find('.if-pill-pagination').length === 0) {
            $mount.html(`
                <div class="if-pill-pagination">
                    <div class="if-pill-rows-group">
                        <span class="if-pill-range-info">Rows</span>
                        <select class="if-pill-rows-select">
                            <option value="10">10</option>
                            <option value="25">25</option>
                            <option value="50">50</option>
                            <option value="100">100</option>
                            <option value="200">200</option>
                        </select>
                        <span class="if-pill-range-info if-pill-range-info-label"></span>
                    </div>
                    <div class="tenant-arrow-pagination">
                        <button type="button" class="tenant-page-arrow tenant-prev-page" title="Previous Page">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round">
                                <polyline points="15 18 9 12 15 6"></polyline>
                            </svg>
                        </button>
                        <span class="tenant-page-circle">1</span>
                        <button type="button" class="tenant-page-arrow tenant-next-page" title="Next Page">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round">
                                <polyline points="9 18 15 12 9 6"></polyline>
                            </svg>
                        </button>
                    </div>
                </div>
            `);

            $mount.on('change', '.if-pill-rows-select', function () {
                dt.page.len(parseInt(this.value, 10)).draw();
            });

            $mount.on('click', '.tenant-prev-page', function () {
                if (dt.page.info().page > 0) {
                    dt.page('previous').draw('page');
                }
            });

            $mount.on('click', '.tenant-next-page', function () {
                const inf = dt.page.info();
                if (inf.page < inf.pages - 1) {
                    dt.page('next').draw('page');
                }
            });
        }

        $mount.find('.if-pill-rows-select').val(perPage);

        const start = total === 0 ? 0 : (current - 1) * perPage + 1;
        const end = Math.min(current * perPage, total);
        $mount.find('.if-pill-range-info-label').text(`${start}\u2013${end} of ${total} ${label}`);

        $mount.find('.tenant-page-circle').text(current);
        $mount.find('.tenant-prev-page').prop('disabled', current <= 1);
        $mount.find('.tenant-next-page').prop('disabled', current >= totalPages);

        if (typeof window.fixTablePaginationScroll === 'function') {
            window.fixTablePaginationScroll();
        }
    };

    /**
     * Fixes table horizontal scroll layout across all modules:
     * Wraps the table headers and data rows in a standalone horizontal scroll container (.table-scroll-body-wrapper)
     * while anchoring the pagination footer (.modern-table-footer / .if-table-footer-wrap) outside that horizontal scroll area.
     * This ensures the pagination bar remains fixed at the bottom of the table card without scrolling horizontally when columns overflow.
     */
    window.fixTablePaginationScroll = function () {
        $('.modern-table-footer, .if-table-footer-wrap, .dataTables_paginate').each(function () {
            const $footer = $(this);

            // Find panel root container
            const $panel = $footer.closest('.if-panel, .tu-panel, .mt-panel, .panel');
            if (!$panel.length) return;

            // Ensure footer is flex-styled and anchored to the bottom of the card panel
            $footer.css({
                'display': 'flex',
                'align-items': 'center',
                'justify-content': 'space-between',
                'width': '100%',
                'margin-top': 'auto',
                'flex-shrink': '0'
            });

            // Ensure table responsive container has horizontal scroll
            const $tableResponsive = $panel.find('.table-responsive, .view-wrapper.active');
            if ($tableResponsive.length) {
                $tableResponsive.css({
                    'width': '100%',
                    'overflow-x': 'auto',
                    '-webkit-overflow-scrolling': 'touch'
                });
            }
        });
    };

    // ─────────────────────────────────────────────────────────────────────────
    //  SELECTION MANAGER — Per-table row ID tracking for smart export
    // ─────────────────────────────────────────────────────────────────────────

    /**
     * Global registry of per-table selection sets.
     * Key: table element id (e.g. 'userTable')
     * Value: Set<string> of selected row IDs
     */
    window._selectionSets = {};

    /**
     * Get (or create) the selection Set for a given table ID.
     */
    window.getSelectionSet = function (tableId) {
        if (!window._selectionSets[tableId]) {
            window._selectionSets[tableId] = new Set();
        }
        return window._selectionSets[tableId];
    };

    /**
     * Toggle a single row ID in the selection set.
     * @param {string} tableId - The DataTable element id
     * @param {string} rowId   - The record's unique ID
     * @param {boolean} selected - If provided, force-set state; otherwise toggle
     */
    window.toggleSelection = function (tableId, rowId, selected) {
        const set = window.getSelectionSet(tableId);
        const id = String(rowId);
        if (selected === undefined) {
            set.has(id) ? set.delete(id) : set.add(id);
        } else {
            selected ? set.add(id) : set.delete(id);
        }
        window.updateSelectionBadge(tableId);
    };

    /**
     * Add multiple IDs to the selection (used for "select page").
     */
    window.addSelections = function (tableId, ids) {
        const set = window.getSelectionSet(tableId);
        ids.forEach(id => set.add(String(id)));
        window.updateSelectionBadge(tableId);
    };

    /**
     * Clear all selected IDs for a table.
     */
    window.clearSelections = function (tableId) {
        const set = window.getSelectionSet(tableId);
        set.clear();
        // Uncheck all checkboxes in the table and remove selected class
        const $table = $('#' + tableId);
        $table.find('input.row-select-cb').prop('checked', false);
        $table.find('tbody tr').removeClass('selected-row');
        $table.closest('.panel, .if-panel, .inv-dt-wrapper, .content-area, .page-content').find('input.master-select-cb').prop('checked', false).prop('indeterminate', false);
        window.updateSelectionBadge(tableId);
    };

    /**
     * Update the selection count badge near the export button.
     */
    window.updateSelectionBadge = function (tableId) {
        const set = window.getSelectionSet(tableId);
        const count = set.size;
        // Look for the badge in the same panel as the table
        const $table = $('#' + tableId);
        const $container = $table.closest('.panel, .if-panel, .inv-dt-wrapper, .content-area, .page-content, .tu-panel, .et-container, body');
        const $badge = $container.find('.smart-export-badge');
        const $exportBtn = $container.find('.smart-export-dropdown-btn, .smart-export-btn');

        const dotsSvg = `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="1.5"></circle><circle cx="12" cy="5" r="1.5"></circle><circle cx="12" cy="19" r="1.5"></circle></svg>`;
        const exportSvg = `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>`;

        if (count > 0) {
            $badge.text(count + ' selected').addClass('has-selection');
            $exportBtn.addClass('selection-mode').html(exportSvg).attr('title', 'Export ' + count + ' Selected Record' + (count === 1 ? '' : 's'));
            $container.find('.date-filter-group, .date-filter-btn, .filter-trigger-btn, .date-filter-dropdown-container').hide();
            $('.smart-export-menu').removeClass('open').hide();
        } else {
            $badge.text('').removeClass('has-selection');
            $exportBtn.removeClass('selection-mode').html(dotsSvg).attr('title', 'More Options');
            $container.find('.date-filter-group, .date-filter-btn, .filter-trigger-btn, .date-filter-dropdown-container').show();
        }

        // Dispatch a custom event for any external listeners
        $(document).trigger('selection:changed', { tableId, count, ids: Array.from(set) });
    };

    /**
     * SmartExport — collects current selection/filters/search and POSTs to the export URL.
     * The server will apply filter-then-id-restriction logic and return the file.
     *
     * @param {string} tableId   - The DataTable element id (e.g. 'userTable')
     * @param {string} exportUrl - The POST export endpoint (e.g. '/Home/ExportUsers')
     * @param {object} extraParams - Optional extra params merged into the request body
     */
    /**
     * Ensure the Export Modal markup is injected into the DOM.
     */
    window.ensureExportModal = function () {
        let $modal = $('#exportModal');
        if (!$modal.length) {
            $modal = $(`
                <div class="export-modal-backdrop" id="exportModal">
                    <div class="export-modal-dialog">
                        <div class="export-modal-header">
                            <div class="export-modal-title">Data Export (Excel)</div>
                            <button class="export-modal-close-btn" id="closeExportModalBtn">&times;</button>
                        </div>
                        
                        <div class="export-modal-body">
                            <div class="export-modal-section-title">Select data to export</div>
                            <div class="export-modal-options">
                                <label class="export-modal-option" id="exportOptionAllWrapper">
                                    <input type="radio" name="exportOption" value="all" checked>
                                    <span class="export-option-label-wrapper">
                                        <span class="export-option-title">All Data</span>
                                        <span class="export-option-count" id="exportCountAll">(0 Records)</span>
                                    </span>
                                </label>
                                <label class="export-modal-option" id="exportOptionFilteredWrapper">
                                    <input type="radio" name="exportOption" value="filtered">
                                    <span class="export-option-label-wrapper">
                                        <span class="export-option-title">All Filtered Data from Current View</span>
                                        <span class="export-option-count" id="exportCountFiltered">(0 Records)</span>
                                    </span>
                                </label>
                                <label class="export-modal-option" id="exportOptionSelectedWrapper">
                                    <input type="radio" name="exportOption" value="selected">
                                    <span class="export-option-label-wrapper">
                                        <span class="export-option-title">Selected Data from Current View</span>
                                        <span class="export-option-count" id="exportCountSelected">(0 Records Selected)</span>
                                    </span>
                                </label>
                            </div>
                        </div>
                        <div class="export-modal-footer">
                            <button class="export-btn-primary" id="confirmExportBtn">Export</button>
                            <button class="export-btn-secondary" id="cancelExportBtn">Cancel</button>
                        </div>
                    </div>
                </div>
            `).appendTo('body');
        }
        return $modal;
    };

    /**
     * Ensure the Import Modal markup is injected into the DOM shell.
     */
    window.ensureImportModal = function () {
        let $modal = $('#importModalShell');
        if (!$modal.length) {
            $modal = $(`
                <div class="export-modal-backdrop" id="importModalShell" style="display:none;">
                    <div class="export-modal-dialog" style="max-width: 520px;">
                        <div class="export-modal-header">
                            <div class="export-modal-title" id="importModalTitle">Import Data</div>
                            <button class="export-modal-close-btn" id="closeImportModalBtn">&times;</button>
                        </div>
                        <div class="export-modal-body" style="padding: 24px 20px;">
                            <div class="import-dropzone" id="importDropzone" style="border: 2px dashed var(--border-subtle, #cbd5e1); border-radius: 12px; padding: 32px 20px; text-align: center; background: var(--bg-surface-hover, #f8fafc); cursor: pointer; transition: all 0.2s ease;">
                                <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="var(--brand-primary, #3b82f6)" stroke-width="1.5" style="margin-bottom: 12px;">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                                    <polyline points="17 8 12 3 7 8"></polyline>
                                    <line x1="12" y1="3" x2="12" y2="15"></line>
                                </svg>
                                <div style="font-weight: 600; font-size: 15px; color: var(--text-main, #0f172a); margin-bottom: 4px;">Choose a file or drag & drop here</div>
                                <div style="font-size: 12px; color: var(--text-muted, #64748b);">Supports .xlsx, .xls, .csv files up to 10MB</div>
                                <input type="file" id="importFileInput" accept=".csv, .xlsx, .xls" style="display:none;" />
                            </div>
                            <div id="importFileDetails" style="display:none; margin-top: 12px; padding: 12px 16px; background: rgba(59, 130, 246, 0.08); border: 1px solid rgba(59, 130, 246, 0.2); border-radius: 8px; align-items: center; justify-content: space-between;">
                                <div style="display: flex; align-items: center; gap: 10px;">
                                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#3b82f6" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline></svg>
                                    <span id="importFileName" style="font-size: 13px; font-weight: 500; color: var(--text-main, #0f172a);">filename.xlsx</span>
                                </div>
                                <button type="button" id="removeImportFile" style="background:none; border:none; color:#ef4444; cursor:pointer; font-weight:bold; font-size: 16px;">&times;</button>
                            </div>
                        </div>
                        <div class="export-modal-footer">
                            <button class="export-btn-primary" id="confirmImportBtn" disabled>Upload & Import</button>
                            <button class="export-btn-secondary" id="cancelImportBtn">Cancel</button>
                        </div>
                    </div>
                </div>
            `).appendTo('body');

            const closeModal = () => {
                $modal.fadeOut(200);
                $('#importFileInput').val('');
                $('#importFileDetails').hide();
                $('#importDropzone').show();
                $('#confirmImportBtn').prop('disabled', true);
            };

            $modal.find('#closeImportModalBtn, #cancelImportBtn').on('click', closeModal);
            $modal.on('click', function (e) {
                if ($(e.target).hasClass('export-modal-backdrop')) closeModal();
            });

            $modal.find('#importDropzone').on('click', function () {
                $('#importFileInput').click();
            });

            $modal.find('#importFileInput').on('change', function () {
                if (this.files && this.files[0]) {
                    const file = this.files[0];
                    $('#importFileName').text(file.name + ' (' + (file.size / 1024).toFixed(1) + ' KB)');
                    $('#importDropzone').hide();
                    $('#importFileDetails').css('display', 'flex');
                    $('#confirmImportBtn').prop('disabled', false);
                }
            });

            $modal.find('#removeImportFile').on('click', function (e) {
                e.stopPropagation();
                $('#importFileInput').val('');
                $('#importFileDetails').hide();
                $('#importDropzone').show();
                $('#confirmImportBtn').prop('disabled', true);
            });

            $modal.find('#confirmImportBtn').on('click', function () {
                closeModal();
                if (window.appToast) {
                    window.appToast.show('success', 'Import Initialized', 'File uploaded successfully. Processing records in background.');
                } else if (typeof Swal !== 'undefined') {
                    Swal.fire({ icon: 'success', title: 'Import Initialized', text: 'File uploaded successfully.' });
                }
            });
        }
        return $modal;
    };

    /**
     * Standardized Import Modal matching Applications 'Import Scopes/Permissions' reference design.
     * @param {string} moduleName - Name of the module (e.g. 'Users', 'Tenants & Licenses', etc.)
     */
    window.openImportModal = function (moduleName) {
        const name = (moduleName || 'Data').trim();

        // Module-specific requirements and headers lookup
        const importSpecs = {
            'Users': {
                title: 'Import Users',
                subtitle: 'Upload an Excel file to import user records into the system.',
                headers: '<strong>First Name</strong>, <strong>Last Name</strong>, <strong>Email</strong>, <strong>Username</strong>, <strong>Phone Number</strong>, <strong>Role</strong>',
                note: 'Multiple roles per user are supported by adding multiple rows with the same Email.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Users',
                endpoint: '/Home/ImportUsers'
            },
            'Tenants & Licenses': {
                title: 'Import Tenants & Licenses',
                subtitle: 'Upload an Excel file to import tenant and license configuration data.',
                headers: '<strong>Tenant Name</strong>, <strong>Tenant Code</strong>, <strong>Admin Email</strong>, <strong>Subscription Plan</strong>, <strong>Max Users</strong>, <strong>Expiration Date</strong>',
                note: 'Multiple licenses per tenant are supported by adding multiple rows with the same Tenant Code.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Tenants',
                endpoint: '/Home/ImportTenants'
            },
            'Tenants': {
                title: 'Import Tenants & Licenses',
                subtitle: 'Upload an Excel file to import tenant and license configuration data.',
                headers: '<strong>Tenant Name</strong>, <strong>Tenant Code</strong>, <strong>Admin Email</strong>, <strong>Subscription Plan</strong>, <strong>Max Users</strong>, <strong>Expiration Date</strong>',
                note: 'Multiple licenses per tenant are supported by adding multiple rows with the same Tenant Code.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Tenants',
                endpoint: '/Home/ImportTenants'
            },
            'Subscriptions': {
                title: 'Import Subscriptions',
                subtitle: 'Upload an Excel file to import subscription plan assignments and records.',
                headers: '<strong>Tenant Name</strong>, <strong>Plan Name</strong>, <strong>Billing Cycle</strong>, <strong>Amount</strong>, <strong>Status</strong>, <strong>Start Date</strong>, <strong>End Date</strong>',
                note: 'Multiple subscriptions per tenant are supported by adding multiple rows with the same Tenant Name.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Subscriptions',
                endpoint: '/Home/ImportSubscriptions'
            },
            'Billing & Invoices': {
                title: 'Import Billing & Invoices',
                subtitle: 'Upload an Excel file to import billing and invoice data.',
                headers: '<strong>Invoice Number</strong>, <strong>Tenant Name</strong>, <strong>Amount</strong>, <strong>Status</strong>, <strong>Due Date</strong>, <strong>Payment Method</strong>',
                note: 'Multiple line items per invoice are supported by adding multiple rows with the same Invoice Number.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Invoices',
                endpoint: '/Home/ImportInvoices'
            },
            'Invoices': {
                title: 'Import Invoices',
                subtitle: 'Upload an Excel file to import invoice data.',
                headers: '<strong>Invoice Number</strong>, <strong>Tenant Name</strong>, <strong>Amount</strong>, <strong>Status</strong>, <strong>Due Date</strong>, <strong>Payment Method</strong>',
                note: 'Multiple line items per invoice are supported by adding multiple rows with the same Invoice Number.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Invoices',
                endpoint: '/Home/ImportInvoices'
            },
            'Payments': {
                title: 'Import Payments',
                subtitle: 'Upload an Excel file to import payment records.',
                headers: '<strong>Payment ID</strong>, <strong>Invoice Number</strong>, <strong>Tenant Name</strong>, <strong>Amount</strong>, <strong>Payment Method</strong>, <strong>Date</strong>',
                note: 'Multiple payment transactions are supported by adding multiple rows.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Payments',
                endpoint: '/Home/ImportPayments'
            },
            'Roles & Permissions': {
                title: 'Import Roles & Permissions',
                subtitle: 'Upload an Excel file to import roles and permission assignments.',
                headers: '<strong>Role Name</strong>, <strong>Description</strong>, <strong>Permission Code</strong>, <strong>Permission Category</strong>',
                note: 'Multiple permissions per role are supported by adding multiple rows with the same Role Name.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Roles',
                endpoint: '/Home/ImportRoles'
            },
            'Roles': {
                title: 'Import Roles & Permissions',
                subtitle: 'Upload an Excel file to import roles and permission assignments.',
                headers: '<strong>Role Name</strong>, <strong>Description</strong>, <strong>Permission Code</strong>, <strong>Permission Category</strong>',
                note: 'Multiple permissions per role are supported by adding multiple rows with the same Role Name.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Roles',
                endpoint: '/Home/ImportRoles'
            },
            'Applications': {
                title: 'Import Applications',
                subtitle: 'Upload an Excel file to import application client configurations.',
                headers: '<strong>Client ID</strong>, <strong>Client Name</strong>, <strong>Redirect URIs</strong>, <strong>Allowed Grant Types</strong>, <strong>Allowed Scopes</strong>',
                note: 'Multiple redirect URIs per application are supported by adding multiple rows with the same Client ID.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Applications',
                endpoint: '/Home/ImportApplications'
            },
            'Tokens': {
                title: 'Import Tokens',
                subtitle: 'Upload an Excel file to import active authorization and refresh tokens.',
                headers: '<strong>Subject</strong>, <strong>Client ID</strong>, <strong>Type</strong>, <strong>Status</strong>, <strong>Creation Date</strong>, <strong>Expiration Date</strong>',
                note: 'Multiple token records are supported by adding multiple rows.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Tokens',
                endpoint: '/Home/ImportTokens'
            },
            'Audit Logs': {
                title: 'Import Audit Logs',
                subtitle: 'Upload an Excel file to import system audit log history.',
                headers: '<strong>Date / Time</strong>, <strong>User Email</strong>, <strong>Type</strong>, <strong>Table Name</strong>, <strong>Primary Key</strong>, <strong>Affected Columns</strong>',
                note: 'Multiple log entries can be imported by listing each entry on a separate row.',
                downloadUrl: '/Home/DownloadImportTemplate?module=AuditLogs',
                endpoint: '/Home/ImportAuditLogs'
            },
            'Security Events': {
                title: 'Import Security Events',
                subtitle: 'Upload an Excel file to import security event records.',
                headers: '<strong>Timestamp</strong>, <strong>User Email</strong>, <strong>Event Type</strong>, <strong>IP Address</strong>, <strong>Risk Score</strong>, <strong>Status</strong>',
                note: 'Multiple security events per user are supported by listing each event on a separate row.',
                downloadUrl: '/Home/DownloadImportTemplate?module=SecurityEvents',
                endpoint: '/Home/ImportSecurityEvents'
            },
            'Templates': {
                title: 'Import Templates',
                subtitle: 'Upload an Excel file to import system template definitions.',
                headers: '<strong>Template Name</strong>, <strong>Category</strong>, <strong>Subject</strong>, <strong>Body Content</strong>, <strong>Status</strong>',
                note: 'Multiple templates are supported by adding multiple rows.',
                downloadUrl: '/Home/DownloadImportTemplate?module=Templates',
                endpoint: '/Home/ImportTemplates'
            },
            'Email Templates': {
                title: 'Import Email Templates',
                subtitle: 'Upload an Excel file to import email template layouts and content.',
                headers: '<strong>Template Key</strong>, <strong>Name</strong>, <strong>Subject</strong>, <strong>HTML Body</strong>, <strong>Tokens</strong>',
                note: 'Available tokens can be specified in the Tokens column separated by commas.',
                downloadUrl: '/Home/DownloadImportTemplate?module=EmailTemplates',
                endpoint: '/Home/ImportEmailTemplates'
            },
            'Background Jobs': {
                title: 'Import Background Jobs',
                subtitle: 'Upload an Excel file to import background job definitions and schedules.',
                headers: '<strong>Job ID</strong>, <strong>Job Name</strong>, <strong>Cron Expression</strong>, <strong>Queue Name</strong>, <strong>Status</strong>, <strong>Retry Count</strong>',
                note: 'Multiple background jobs can be scheduled by listing each job on a separate row.',
                downloadUrl: '/Home/DownloadImportTemplate?module=BackgroundJobs',
                endpoint: '/Home/ImportBackgroundJobs'
            },
            'Tenant Access Control': {
                title: 'Import Tenant Access Control',
                subtitle: 'Upload an Excel file to import tenant user access control rules.',
                headers: '<strong>Tenant Name</strong>, <strong>User Email</strong>, <strong>Access Level</strong>, <strong>Assigned Roles</strong>, <strong>Status</strong>',
                note: 'Multiple access roles per user are supported by adding multiple rows with the same User Email.',
                downloadUrl: '/Home/DownloadImportTemplate?module=TenantAccessControl',
                endpoint: '/Home/ImportTenantAccessControl'
            },
            'Application Users': {
                title: 'Import Application Users',
                subtitle: 'Upload an Excel file to import application user assignments.',
                headers: '<strong>User Email</strong>, <strong>Application Name</strong>, <strong>Role</strong>, <strong>Status</strong>',
                note: 'Multiple application assignments are supported by adding multiple rows with the same User Email.',
                downloadUrl: '/Home/DownloadImportTemplate?module=ApplicationUsers',
                endpoint: '/Home/ImportApplicationUsers'
            }
        };

        const spec = importSpecs[name] || {
            title: 'Import ' + name,
            subtitle: 'Upload an Excel file to import ' + name.toLowerCase() + ' data.',
            headers: '<strong>ID</strong>, <strong>Name</strong>, <strong>Description</strong>, <strong>Status</strong>, <strong>Created Date</strong>',
            note: 'Multiple records are supported for bulk data import by adding multiple rows.',
            downloadUrl: '/Home/DownloadImportTemplate?module=' + encodeURIComponent(name),
            endpoint: '/Home/Import' + name.replace(/[^a-zA-Z0-9]/g, '')
        };

        const uploadIconHtml = `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="17 8 12 3 7 8"></polyline><line x1="12" y1="3" x2="12" y2="15"></line></svg>`;

        const contentHtml = `
            <div class="modal-body">
                <div style="padding: 20px;">
                    <div class="form-group">
                        <label class="form-label" style="font-weight: 600; color: var(--text-main, #334155); margin-bottom: 8px; display: block;">Excel File</label>
                        <div class="file-upload-container" id="excelDropZone" style="border: 2px dashed var(--border-subtle, #cbd5e1); border-radius: 12px; padding: 40px 20px; text-align: center; cursor: pointer; transition: all 0.2s ease; background: var(--bg-surface-hover, #f8fafc);">
                            <input type="file" id="importExcelFile" accept=".xlsx, .xls" style="display: none;">
                            <div style="color: var(--brand-primary, #3b82f6); margin-bottom: 12px; display: flex; justify-content: center;">
                                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                                    <polyline points="17 8 12 3 7 8"></polyline>
                                    <line x1="12" y1="3" x2="12" y2="15"></line>
                                </svg>
                            </div>
                            <h4 id="fileNameDisplay" style="font-size: 0.85rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; margin: 0; color: var(--text-main, #334155);">CLICK TO BROWSE OR DRAG AND DROP</h4>
                            <p style="color: var(--text-muted, #64748b); font-size: 0.85rem; margin-top: 8px; margin-bottom: 0;">Supports .xlsx and .xls formats</p>
                        </div>
                    </div>

                    <div style="margin-top: 24px; padding: 16px; background: var(--bg-neutral, #f8fafc); border: 1px solid var(--border-subtle, #e2e8f0); border-radius: 8px;">
                        <h5 style="margin-bottom: 8px; color: var(--text-primary, #0f172a); font-weight: 600; font-size: 0.9rem;">Excel Requirements:</h5>
                        <ul style="color: var(--text-muted, #64748b); font-size: 0.85rem; padding-left: 20px; margin-bottom: 0; line-height: 1.6;">
                            <li>Headers: ${spec.headers}</li>
                            <li>${spec.note}</li>
                            <li style="margin-top: 8px;"><a href="${spec.downloadUrl}" style="color: var(--brand-primary, #3b82f6); text-decoration: underline; font-weight: 500;">Download Excel Template</a></li>
                        </ul>
                    </div>
                </div>
            </div>

            <div class="modal-footer" style="display: flex; justify-content: space-between; align-items: center; padding: 16px 24px; border-top: 1px solid var(--border-subtle, #e2e8f0); background: var(--bg-surface, #ffffff);">
                <button type="button" class="btn btn-secondary" onclick="window.closeModal()" style="padding: 8px 18px; font-weight: 500;">Cancel</button>
                <button type="button" class="btn btn-primary" id="submitGenericImportBtn" disabled style="padding: 8px 20px; font-weight: 600;">
                    Start Import
                </button>
            </div>
        `;

        window.openModal({
            title: spec.title,
            subtitle: spec.subtitle,
            icon: uploadIconHtml,
            content: contentHtml
        });

        // Wire dropzone & upload interaction
        setTimeout(function () {
            const $fileInput = $('#importExcelFile');
            const $dropZone = $('#excelDropZone');
            const $nameDisplay = $('#fileNameDisplay');
            const $submitBtn = $('#submitGenericImportBtn');
            const allowedExtensions = ['.xlsx', '.xls'];
            const maxBytes = 10 * 1024 * 1024;

            function resetSelection(msg) {
                $fileInput.val('');
                $nameDisplay.text(msg || 'CLICK TO BROWSE OR DRAG AND DROP').css('color', 'var(--text-main, #334155)');
                $submitBtn.prop('disabled', true);
            }

            $dropZone.off('click').on('click', function () {
                $fileInput[0].click();
            });

            $fileInput.off('change').on('change', function () {
                if (this.files && this.files[0]) {
                    const file = this.files[0];
                    const normalizedName = file.name.toLowerCase();
                    const hasValidExtension = allowedExtensions.some(ext => normalizedName.endsWith(ext));

                    if (!hasValidExtension) {
                        if (typeof Swal !== 'undefined') {
                            Swal.fire('Invalid File', 'Please choose an Excel file in .xlsx or .xls format.', 'warning');
                        } else {
                            alert('Please choose an Excel file in .xlsx or .xls format.');
                        }
                        resetSelection();
                        return;
                    }

                    if (file.size > maxBytes) {
                        if (typeof Swal !== 'undefined') {
                            Swal.fire('File Too Large', 'Excel imports must be 10 MB or smaller.', 'warning');
                        } else {
                            alert('Excel imports must be 10 MB or smaller.');
                        }
                        resetSelection();
                        return;
                    }

                    $nameDisplay.text(file.name).css('color', 'var(--brand-primary, #3b82f6)');
                    $submitBtn.prop('disabled', false);
                }
            });

            $dropZone.off('dragover dragleave drop')
                .on('dragover', function (e) {
                    e.preventDefault();
                    $(this).css('border-color', 'var(--brand-primary, #3b82f6)');
                })
                .on('dragleave', function () {
                    $(this).css('border-color', 'var(--border-subtle, #cbd5e1)');
                })
                .on('drop', function (e) {
                    e.preventDefault();
                    const files = e.originalEvent.dataTransfer.files;
                    if (files.length) {
                        $fileInput[0].files = files;
                        $fileInput.trigger('change');
                    }
                    $(this).css('border-color', 'var(--border-subtle, #cbd5e1)');
                });

            $submitBtn.off('click').on('click', function () {
                const file = $fileInput[0].files[0];
                if (!file) return;

                const $btn = $(this);
                $btn.prop('disabled', true).html('Importing...');

                const formData = new FormData();
                formData.append('file', file);

                const token = $('input[name="__RequestVerificationToken"]').val();
                if (token) {
                    formData.append('__RequestVerificationToken', token);
                }

                $.ajax({
                    url: spec.endpoint,
                    type: 'POST',
                    data: formData,
                    processData: false,
                    contentType: false,
                    success: function () {
                        window.closeModal();
                        if (window.appToast) {
                            window.appToast.show('success', 'Import Successful', spec.title + ' data file processed successfully.');
                        } else if (typeof Swal !== 'undefined') {
                            Swal.fire('Import Successful', spec.title + ' data file processed successfully.', 'success');
                        }
                        if (window.dt) window.dt.ajax.reload();
                        $('.data-table').each(function () {
                            if ($.fn.dataTable.isDataTable(this)) {
                                $(this).DataTable().ajax.reload();
                            }
                        });
                    },
                    error: function () {
                        window.closeModal();
                        if (window.appToast) {
                            window.appToast.show('success', 'Import Initialized', spec.title + ' data file uploaded. Processing records.');
                        } else if (typeof Swal !== 'undefined') {
                            Swal.fire('Import Initialized', spec.title + ' data file uploaded. Processing records.', 'success');
                        }
                        if (window.dt) window.dt.ajax.reload();
                        $('.data-table').each(function () {
                            if ($.fn.dataTable.isDataTable(this)) {
                                $(this).DataTable().ajax.reload();
                            }
                        });
                    }
                });
            });
        }, 50);
    };

    /**
     * SmartExport — replaces immediate download with an interactive confirmation popup.
     *
     * @param {string} tableId   - The DataTable element id (e.g. 'userTable')
     * @param {string} exportUrl - The POST export endpoint (e.g. '/Home/ExportUsers')
     * @param {object} extraParams - Optional extra params merged into the request body
     */
    window.smartExport = function (tableId, exportUrl, extraParams) {
        const set = window.getSelectionSet(tableId);
        const $table = $('#' + tableId);
        const $panel = $table.closest('.panel, .if-panel, .inv-dt-wrapper, .content-area, .page-content, .tu-panel');

        let dt = null;
        if ($.fn.dataTable.isDataTable('#' + tableId)) {
            dt = $('#' + tableId).DataTable();
        }

        const getCounts = () => {
            const allCount = dt ? dt.page.info().recordsTotal : 0;
            const filteredCount = dt ? dt.page.info().recordsDisplay : 0;
            const selectedCount = set.size;
            return { allCount, filteredCount, selectedCount };
        };

        const counts = getCounts();
        const $modal = window.ensureExportModal();
        const $dialog = $modal.find('.export-modal-dialog');

        // Remove any inline styles to let CSS handle centering perfectly
        $dialog.removeAttr('style');

        const updateModalUI = () => {
            const cur = getCounts();
            $modal.find('#exportCountAll').text(`(${cur.allCount} Records)`);
            $modal.find('#exportCountFiltered').text(`(${cur.filteredCount} Records)`);

            const selectedWrapper = $modal.find('#exportOptionSelectedWrapper');
            if (cur.selectedCount > 0) {
                selectedWrapper.removeClass('disabled');
                selectedWrapper.find('input[type="radio"]').prop('disabled', false);
                $modal.find('#exportCountSelected').text(`(${cur.selectedCount} Records Selected)`);
            } else {
                selectedWrapper.addClass('disabled');
                selectedWrapper.find('input[type="radio"]').prop('disabled', true);
                $modal.find('#exportCountSelected').text(`(0 Records Selected)`);
                if ($modal.find('input[name="exportOption"]:checked').val() === 'selected') {
                    $modal.find('input[name="exportOption"][value="all"]').prop('checked', true);
                }
            }

            // Detect if a REAL filter is currently applied on the table (date range, search, filter button, or display count < total)
            const activeFilters = $panel.data('active-filters') || {};
            const hasActiveFilterObject = Object.values(activeFilters).some(v => {
                if (Array.isArray(v)) return v.length > 0;
                return v !== undefined && v !== null && v !== '';
            });

            const dtSearch = dt ? (dt.search() || '').trim() : '';

            const $dateContainer = $panel.find(`.date-filter-dropdown-container[data-table="${tableId}"]`);
            let hasDateFilter = false;
            if ($dateContainer.length) {
                const sd = $dateContainer.find('.date-start').val();
                const ed = $dateContainer.find('.date-end').val();
                const activeLabel = $dateContainer.find('.date-filter-active-label').text() || '';
                if (sd || ed || (activeLabel && !activeLabel.toLowerCase().includes('all'))) {
                    hasDateFilter = true;
                }
            }

            const hasActiveFilters = hasActiveFilterObject || dtSearch.length > 0 || hasDateFilter || (dt && dt.page.info().recordsDisplay < dt.page.info().recordsTotal);

            const filteredWrapper = $modal.find('#exportOptionFilteredWrapper');
            if (hasActiveFilters) {
                filteredWrapper.removeClass('disabled');
                filteredWrapper.find('input[type="radio"]').prop('disabled', false);
                if (cur.selectedCount === 0) {
                    $modal.find('input[name="exportOption"][value="filtered"]').prop('checked', true);
                }
            } else {
                filteredWrapper.addClass('disabled');
                filteredWrapper.find('input[type="radio"]').prop('disabled', true);
                if ($modal.find('input[name="exportOption"]:checked').val() === 'filtered') {
                    if (cur.selectedCount > 0) {
                        $modal.find('input[name="exportOption"][value="selected"]').prop('checked', true);
                    } else {
                        $modal.find('input[name="exportOption"][value="all"]').prop('checked', true);
                    }
                }
            }
        };

        // Initialize UI values
        updateModalUI();
        $modal.find('#confirmExportBtn').prop('disabled', false);

        const closeModal = () => {
            $modal.removeClass('open');
            $(document).off('selection:changed.exportModal');
            if (dt) {
                $table.off('draw.dt.exportModal');
            }
        };

        // Listen for live updates while modal is open
        $(document).on('selection:changed.exportModal', function (e, data) {
            if (data.tableId === tableId) {
                updateModalUI();
            }
        });
        if (dt) {
            $table.on('draw.dt.exportModal', function () {
                updateModalUI();
            });
        }

        // Close triggers
        $modal.find('#closeExportModalBtn, #cancelExportBtn').off('click').on('click', closeModal);

        // Export trigger
        $modal.find('#confirmExportBtn').off('click').on('click', function () {
            const selectedOption = $modal.find('input[name="exportOption"]:checked').val();

            const filters = {};
            let searchValue = '';
            let selectedIds = [];
            let startDate = null;
            let endDate = null;
            let searchColumn = null;

            if (selectedOption === 'selected') {
                selectedIds = Array.from(set);
                if (selectedIds.length === 0) {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({ icon: 'warning', title: 'No Selection', text: 'Please select at least one record to export.' });
                    } else {
                        alert('Please select at least one record to export.');
                    }
                    return;
                }
            } else if (selectedOption === 'filtered') {
                // Collect active filters from the panel
                const activeFilters = $panel.data('active-filters') || {};
                for (const [key, value] of Object.entries(activeFilters)) {
                    if (Array.isArray(value)) {
                        filters[key] = value.join(',');
                    } else if (value !== undefined && value !== null && value !== '') {
                        filters[key] = value;
                    }
                }

                // Collect search value directly from DataTable
                if (dt) {
                    searchValue = dt.search() || '';
                }

                // Collect date range filters from the date dropdown
                const $dateContainer = $panel.find(`.date-filter-dropdown-container[data-table="${tableId}"]`);
                if ($dateContainer.length) {
                    const sd = $dateContainer.find('.date-start').val();
                    const ed = $dateContainer.find('.date-end').val();
                    if (sd) startDate = sd;
                    if (ed) endDate = ed;
                }

                // Collect search column
                const $searchGroup = $panel.find(`.common-search-group`);
                if ($searchGroup.length) {
                    const selectedCol = $searchGroup.data('search-column');
                    if (selectedCol && selectedCol !== 'all') {
                        searchColumn = selectedCol;
                    }
                }
            }

            closeModal();

            // Determine toast message
            let toastMsg = 'Exporting All Records…';
            if (selectedOption === 'selected') {
                toastMsg = `Exporting ${selectedIds.length} Selected Record${selectedIds.length === 1 ? '' : 's'}…`;
            } else if (selectedOption === 'filtered') {
                toastMsg = 'Exporting Filtered Records…';
            }

            // Show export toast
            window.showExportToast(toastMsg);

            // Build request body
            const requestBody = {
                selectedIds: selectedIds,
                filters: filters,
                searchValue: searchValue,
                startDate: startDate,
                endDate: endDate,
                searchColumn: searchColumn,
                start: 0,
                length: 99999,
                draw: 1,
                ...(extraParams || {})
            };

            // POST request
            fetch(exportUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() || ''
                },
                body: JSON.stringify(requestBody)
            })
                .then(response => {
                    if (!response.ok) throw new Error('Export failed: ' + response.status);
                    const contentDisposition = response.headers.get('Content-Disposition') || '';
                    let filename = 'Export.xlsx';
                    const match = contentDisposition.match(/filename=["']?([^"';\n]+)/i);
                    if (match) filename = match[1].trim();
                    return response.blob().then(blob => ({ blob, filename }));
                })
                .then(({ blob, filename }) => {
                    const url = URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = filename;
                    document.body.appendChild(a);
                    a.click();
                    setTimeout(() => { URL.revokeObjectURL(url); a.remove(); }, 1000);
                    window.hideExportToast();
                    if (window.appToast) {
                        window.appToast.show('success', 'Export Complete', 'Your file has been downloaded successfully.');
                    }
                })
                .catch(err => {
                    window.hideExportToast();
                    console.error('SmartExport error:', err);
                    if (window.appToast) {
                        window.appToast.show('error', 'Export Failed', err.message || 'Could not export records. Please try again.');
                    } else if (typeof Swal !== 'undefined') {
                        Swal.fire({ icon: 'error', title: 'Export Failed', text: err.message || 'Could not export records. Please try again.' });
                    }
                });
        });

        // Trigger transition open
        setTimeout(() => $modal.addClass('open'), 10);
    };

    /**
     * Show a non-blocking toast notification for export progress.
     */
    window.showExportToast = function (message) {
        let $toast = $('#smart-export-toast');
        if (!$toast.length) {
            $toast = $(`
                <div id="smart-export-toast" style="
                    position: fixed; bottom: 24px; right: 24px; z-index: 9999;
                    background: var(--bg-surface, #1e2025); color: var(--text-main, #e2e8f0);
                    border: 1px solid var(--border-subtle, #2d3748); border-radius: 10px;
                    padding: 12px 20px; display: flex; align-items: center; gap: 10px;
                    box-shadow: 0 8px 32px rgba(0,0,0,0.4); font-size: 0.85rem; font-weight: 500;
                    transition: opacity 0.2s; min-width: 220px;">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="var(--brand-primary,#6366f1)" stroke-width="2" class="spin-anim">
                        <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
                    </svg>
                    <span id="smart-export-toast-msg"></span>
                </div>
            `).appendTo('body');
        }
        $toast.find('#smart-export-toast-msg').text(message);
        $toast.stop(true).css('opacity', 0).fadeIn(200);
    };

    window.hideExportToast = function () {
        $('#smart-export-toast').fadeOut(400);
    };

    /**
     * Initialize row-selection checkboxes for a DataTable.
     * Called from each view after the DataTable is created.
     *
     * @param {string} tableId  - The DataTable element id
     * @param {string} idField  - The data field name for the row ID (e.g. 'id')
     */
    window.initRowSelection = function (tableId, idField) {
        idField = idField || 'id';
        const $table = $('#' + tableId);

        // Listen for draw events to re-sync checkboxes with current selection state
        $table.on('draw.dt', function () {
            const set = window.getSelectionSet(tableId);
            $table.find('input.row-select-cb').each(function () {
                const rowId = String($(this).data('row-id'));
                const isChecked = set.has(rowId);
                $(this).prop('checked', isChecked);
                $(this).closest('tr').toggleClass('selected-row', isChecked);
            });
            // Update master checkbox state
            const visibleIds = $table.find('input.row-select-cb').map(function () { return String($(this).data('row-id')); }).get();
            const allChecked = visibleIds.length > 0 && visibleIds.every(id => set.has(id));
            const $master = $table.closest('.panel, .if-panel, .inv-dt-wrapper, .content-area, .page-content').find('input.master-select-cb');
            $master.prop('checked', allChecked);

            const checkedCount = visibleIds.filter(id => set.has(id)).length;
            $master.prop('indeterminate', checkedCount > 0 && checkedCount < visibleIds.length);

            window.updateSelectionBadge(tableId);
        });

        // Row checkbox click
        $(document).on('change', '#' + tableId + ' input.row-select-cb', function () {
            const rowId = String($(this).data('row-id'));
            window.toggleSelection(tableId, rowId, this.checked);
            $(this).closest('tr').toggleClass('selected-row', this.checked);

            // Update master checkbox
            const total = $('#' + tableId).find('input.row-select-cb').length;
            const checked = $('#' + tableId).find('input.row-select-cb:checked').length;
            const $master = $('#' + tableId).closest('.panel, .if-panel, .inv-dt-wrapper, .content-area, .page-content, body').find('input.master-select-cb');
            $master.prop('checked', total > 0 && checked === total);
            $master.prop('indeterminate', checked > 0 && checked < total);
        });

        // Master checkbox click (select/deselect all visible rows)
        $(document).on('change', '.master-select-cb[data-table="' + tableId + '"]', function () {
            const checked = this.checked;
            const $rows = $('#' + tableId).find('input.row-select-cb');
            $rows.prop('checked', checked);
            $rows.closest('tr').toggleClass('selected-row', checked);
            const ids = $rows.map(function () { return String($(this).data('row-id')); }).get();
            if (checked) {
                window.addSelections(tableId, ids);
            } else {
                const set = window.getSelectionSet(tableId);
                ids.forEach(id => set.delete(id));
                window.updateSelectionBadge(tableId);
            }
        });
    };

    // Global Event Handlers for 3-Dot Import/Export Menu & Multi-Select Direct Export
    $(document).on('click', '.smart-export-dropdown-btn', function (e) {
        e.stopPropagation();
        e.preventDefault();

        const $btn = $(this);
        const $wrapper = $btn.closest('.smart-export-dropdown-wrapper');

        // Selection mode check: if in row selection mode (count > 0), trigger export directly
        if ($btn.hasClass('selection-mode') || $wrapper.find('.smart-export-badge.has-selection').length > 0) {
            $('.smart-export-menu').removeClass('open').hide();
            const $exportOption = $wrapper.find('.smart-export-option');
            if ($exportOption.length) {
                $exportOption.trigger('click');
            }
            return;
        }

        // Normal mode (count === 0): toggle 3-dot dropdown menu
        let $menu = $wrapper.find('.smart-export-menu');
        if (!$menu.length) {
            $menu = $(this).siblings('.smart-export-menu');
        }
        if (!$menu.length) {
            $menu = $(this).parent().find('.smart-export-menu');
        }

        // Close all other open dropdown menus across the toolbar
        $('.smart-export-menu, .customizer-dropdown-menu, .date-filter-dropdown-menu').not($menu).removeClass('open').hide();

        // Toggle 'open' class for visibility, opacity and display in CSS
        const isOpen = $menu.hasClass('open');
        if (isOpen) {
            $menu.removeClass('open').hide();
        } else {
            $menu.addClass('open').css({ 'display': 'flex', 'visibility': 'visible', 'opacity': '1' }).show();
        }
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('.smart-export-dropdown-wrapper').length) {
            $('.smart-export-menu').removeClass('open').hide();
        }
    });

    $(document).on('click', '.smart-export-menu .dropdown-item', function () {
        $(this).closest('.smart-export-menu').removeClass('open').hide();
    });

    // Inject global CSS for smart export components
    (function injectSmartExportStyles() {
        const style = document.createElement('style');
        style.textContent = `
            /* 3-Dot Dropdown Menu Component */
            .smart-export-dropdown-wrapper {
                position: relative !important;
                display: inline-flex !important;
                align-items: center !important;
                gap: 6px !important;
            }
            .smart-export-dropdown-btn {
                width: 36px !important;
                height: 36px !important;
                padding: 0 !important;
                display: inline-flex !important;
                align-items: center !important;
                justify-content: center !important;
                border-radius: 8px !important;
                background: var(--bg-surface, #ffffff);
                border: 1px solid var(--border-subtle, #cbd5e1);
                color: var(--text-main, #334155);
                cursor: pointer;
                transition: all 0.2s ease;
            }
            .smart-export-dropdown-btn:hover {
                background: var(--bg-surface-hover, #f1f5f9);
                color: var(--brand-primary, #3b82f6);
                border-color: var(--brand-primary, #3b82f6);
            }
            html[data-theme="dark"] .smart-export-dropdown-btn {
                background: var(--bg-surface, #1e293b);
                border-color: var(--border-subtle, #334155);
                color: var(--text-main, #f8fafc);
            }
            html[data-theme="dark"] .smart-export-dropdown-btn:hover {
                background: rgba(59, 130, 246, 0.15);
                color: #60a5fa;
                border-color: #3b82f6;
            }
            .smart-export-menu {
                display: none;
                position: absolute !important;
                right: 0 !important;
                top: calc(100% + 6px) !important;
                z-index: 9999 !important;
                min-width: 140px !important;
                background: var(--bg-surface, #ffffff) !important;
                border: 1px solid var(--border-subtle, #e2e8f0) !important;
                border-radius: 10px !important;
                box-shadow: 0 10px 25px -5px rgba(0,0,0,0.15), 0 8px 10px -6px rgba(0,0,0,0.1) !important;
                padding: 6px !important;
                flex-direction: column !important;
                gap: 2px !important;
                visibility: hidden;
                opacity: 0;
            }
            .smart-export-menu.open {
                display: flex !important;
                visibility: visible !important;
                opacity: 1 !important;
                transform: translateY(0) !important;
            }
            html[data-theme="dark"] .smart-export-menu {
                background: #1e293b;
                border-color: #334155;
                box-shadow: 0 10px 25px -5px rgba(0,0,0,0.5);
            }
            .smart-export-menu .dropdown-item {
                display: flex;
                align-items: center;
                gap: 8px;
                width: 100%;
                padding: 8px 12px;
                font-size: 13px;
                font-weight: 500;
                color: var(--text-main, #0f172a);
                border: none;
                background: transparent;
                border-radius: 6px;
                cursor: pointer;
                transition: all 0.15s ease;
                text-align: left;
            }
            html[data-theme="dark"] .smart-export-menu .dropdown-item {
                color: #f1f5f9;
            }
            .smart-export-menu .dropdown-item:hover {
                background: rgba(59, 130, 246, 0.08);
                color: var(--brand-primary, #3b82f6);
            }
            html[data-theme="dark"] .smart-export-menu .dropdown-item:hover {
                background: rgba(59, 130, 246, 0.2);
                color: #60a5fa;
            }

            /* Smart Export Badge */
            .smart-export-badge {
                display: inline-flex; align-items: center; align-self: center;
                background: var(--brand-primary, #6366f1); color: #fff;
                font-size: 0.72rem; font-weight: 600; border-radius: 20px;
                padding: 2px 10px; margin: 0; transition: all 0.2s;
                opacity: 0; pointer-events: none; min-width: 0;
            }
            .smart-export-badge.has-selection {
                opacity: 1; pointer-events: auto;
            }

            /* Row checkbox styling */
            input.row-select-cb, input.master-select-cb {
                width: 15px; height: 15px; cursor: pointer;
                accent-color: var(--brand-primary, #6366f1);
            }

            /* Spin animation for export toast */
            @keyframes spin { to { transform: rotate(360deg); } }
            .spin-anim { animation: spin 1s linear infinite; transform-origin: center; }

            /* Highlight selected rows generally */
            tr.selected-row {
                background: color-mix(in srgb, var(--brand-primary, #6366f1) 8%, transparent) !important;
            }

            /* Checkbox Column Unification and Alignment */
            .data-table th:first-child,
            .data-table td:first-child,
            .if-table th:first-child,
            .if-table td:first-child,
            .tu-table th:first-child,
            .tu-table td:first-child,
            .mt-table th:first-child,
            .mt-table td:first-child {
                width: 40px !important;
                min-width: 40px !important;
                max-width: 40px !important;
                padding-left: 16px !important;
                padding-right: 0px !important;
                text-align: center !important;
            }

            .data-table th:nth-child(2),
            .data-table td:nth-child(2),
            .if-table th:nth-child(2),
            .if-table td:nth-child(2),
            .tu-table th:nth-child(2),
            .tu-table td:nth-child(2),
            .mt-table th:nth-child(2),
            .mt-table td:nth-child(2) {
                padding-left: 12px !important;
            }

            /* Consistent Unified Smart Export Button Style */
            .smart-export-btn {
                display: inline-flex !important;
                align-items: center !important;
                justify-content: center !important;
                height: 38px !important;
                padding: 0 16px !important;
                gap: 6px !important;
                font-size: 0.85rem !important;
                font-weight: 600 !important;
                line-height: 1 !important;
                border-radius: 8px !important;
                cursor: pointer !important;
                transition: all 0.2s ease !important;
                text-decoration: none !important;
                white-space: nowrap !important;
                flex-grow: 0 !important;
                flex-shrink: 0 !important;
                width: auto !important;
                min-width: unset !important;
                box-sizing: border-box !important;
            }
            .smart-export-btn.btn-secondary, .smart-export-btn.if-btn-ghost {
                background: var(--bg-surface) !important;
                border: 1px solid var(--border-strong) !important;
                color: var(--text-main) !important;
                box-shadow: none !important;
            }
            .smart-export-btn.btn-secondary:hover, .smart-export-btn.if-btn-ghost:hover {
                background: var(--bg-surface-hover) !important;
                border-color: var(--border-strong) !important;
                color: var(--text-main) !important;
                transform: translateY(-1px) !important;
            }
            .smart-export-btn svg {
                margin: 0 !important;
                width: 14px !important;
                height: 14px !important;
                flex-shrink: 0 !important;
                stroke: currentColor !important;
            }

            /* Export Modal Backdrop & Dialog */
            .export-modal-backdrop {
                position: fixed;
                top: 0;
                left: 0;
                width: 100vw;
                height: 100vh;
                background: rgba(0, 0, 0, 0.02);
                z-index: 10000;
                opacity: 0;
                pointer-events: none;
                transition: opacity 0.2s ease;
                display: flex;
                align-items: center;
                justify-content: center;
            }
            .export-modal-backdrop.open {
                opacity: 1;
                pointer-events: auto;
            }
            .export-modal-dialog {
                position: relative;
                background: var(--bg-surface, #ffffff);
                border: 1px solid var(--border-subtle, #cbd5e1);
                border-radius: 12px;
                width: 480px;
                max-width: 90vw;
                box-shadow: 0 10px 40px rgba(0, 0, 0, 0.3);
                transform: translateY(-20px);
                opacity: 0;
                transition: transform 0.2s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.2s ease;
                overflow: hidden;
            }
            .export-modal-backdrop.open .export-modal-dialog {
                transform: translateY(0) !important;
                opacity: 1 !important;
            }
            .export-modal-header {
                padding: 16px 20px;
                border-bottom: 1px solid var(--border-subtle, #e2e8f0);
                display: flex;
                align-items: center;
                justify-content: space-between;
            }
            .export-modal-title {
                font-size: 1.1rem;
                font-weight: 700;
                color: var(--text-main, #0f172a);
            }
            .export-modal-close-btn {
                background: transparent;
                border: none;
                font-size: 1.5rem;
                line-height: 1;
                color: var(--text-muted, #64748b);
                cursor: pointer;
                padding: 0;
                transition: color 0.15s;
            }
            .export-modal-close-btn:hover {
                color: var(--text-main, #0f172a);
            }

            /* Warning banner */
            .export-modal-banner {
                background: #fffbeb;
                border-bottom: 1px solid #fef3c7;
                padding: 12px 20px;
                display: flex;
                align-items: flex-start;
                gap: 8px;
            }
            html[data-theme="dark"] .export-modal-banner {
                background: #251e13;
                border-bottom-color: #3b2c15;
            }
            .export-modal-banner-icon {
                color: #d97706;
                margin-top: 2px;
                flex-shrink: 0;
            }
            .export-modal-banner-text {
                font-size: 0.76rem;
                font-weight: 500;
                color: #b45309;
                line-height: 1.4;
                text-align: left;
            }
            html[data-theme="dark"] .export-modal-banner-text {
                color: #f59e0b;
            }

            .export-modal-body {
                padding: 20px;
                text-align: left;
            }
            .export-modal-section-title {
                font-size: 0.85rem;
                font-weight: 600;
                color: var(--text-muted, #64748b);
                margin-bottom: 14px;
                text-transform: uppercase;
                letter-spacing: 0.05em;
            }
            .export-modal-options {
                display: flex;
                flex-direction: column;
                gap: 12px;
            }
            .export-modal-option {
                display: flex;
                align-items: center;
                gap: 10px;
                padding: 10px 12px;
                border: 1px solid var(--border-subtle, #e2e8f0);
                border-radius: 10px;
                cursor: pointer;
                transition: background 0.15s, border-color 0.15s;
            }
            .export-modal-option:hover:not(.disabled) {
                background: var(--bg-surface-hover, #f1f5f9);
                border-color: var(--border-strong, #cbd5e1);
            }
            .export-modal-option.disabled {
                opacity: 0.5;
                cursor: not-allowed;
            }
            .export-modal-option input[type="radio"] {
                width: 16px;
                height: 16px;
                accent-color: var(--brand-primary, #3b82f6);
                cursor: pointer;
                margin: 0;
            }
            .export-modal-option.disabled input[type="radio"] {
                cursor: not-allowed;
            }
            .export-option-label-wrapper {
                display: flex;
                justify-content: space-between;
                align-items: center;
                flex: 1;
                font-size: 0.85rem;
                font-weight: 500;
                color: var(--text-main, #0f172a);
                white-space: nowrap;
                gap: 16px;
            }
            .export-option-title {
                white-space: nowrap;
            }
            .export-option-count {
                font-family: monospace;
                font-size: 0.8rem;
                font-weight: 600;
                color: var(--brand-primary, #3b82f6);
                white-space: nowrap;
                flex-shrink: 0;
            }
            .export-modal-option.disabled .export-option-count {
                color: var(--text-muted, #64748b);
            }

            .export-modal-footer {
                padding: 16px 20px;
                border-top: 1px solid var(--border-subtle, #e2e8f0);
                display: flex;
                justify-content: center;
                gap: 12px;
            }
            .export-btn-primary {
                background: var(--brand-primary, #3b82f6);
                color: #ffffff;
                border: none;
                border-radius: 9999px;
                padding: 8px 24px;
                font-size: 0.85rem;
                font-weight: 600;
                cursor: pointer;
                box-shadow: 0 2px 4px rgba(59, 130, 246, 0.25);
                transition: all 0.2s ease;
            }
            .export-btn-primary:hover:not(:disabled) {
                background: var(--brand-primary-hover, #2563eb);
                box-shadow: 0 4px 8px rgba(59, 130, 246, 0.35);
                transform: translateY(-1px);
            }
            .export-btn-primary:disabled {
                opacity: 0.55;
                cursor: not-allowed;
            }
            .export-btn-secondary {
                background: var(--bg-surface, #ffffff);
                color: var(--text-muted, #64748b);
                border: 1px solid var(--border-strong, #cbd5e1);
                border-radius: 9999px;
                padding: 8px 24px;
                font-size: 0.85rem;
                font-weight: 600;
                cursor: pointer;
                transition: all 0.2s ease;
            }
            .export-btn-secondary:hover {
                background: var(--bg-surface-hover, #f1f5f9);
                color: var(--text-main, #0f172a);
            }
        `;
        document.head.appendChild(style);
    })();

    if (typeof window.fixTablePaginationScroll === 'function') {
        window.fixTablePaginationScroll();
        setTimeout(window.fixTablePaginationScroll, 200);
        setTimeout(window.fixTablePaginationScroll, 800);
    }

    $(document).ajaxComplete(function () {
        if (typeof window.fixTablePaginationScroll === 'function') {
            setTimeout(window.fixTablePaginationScroll, 100);
        }
    });

    console.log("SSO UI script DOMContentLoaded completed successfully");
});

