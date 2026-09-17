/**
 * Jobs Management - Interactive Logic
 */
$(document).ready(function() {
    // 1. Core DataTables Initialization
    const jobsTable = $('#jobsTable').DataTable({
        responsive: true,
        pageLength: 15,
        order: [[0, 'asc']],
        dom: '<"top"f>rt<"bottom"lip><"clear">',
        language: {
            search: "",
            searchPlaceholder: "Search services...",
            paginate: {
                previous: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15 18 9 12 15 6"></polyline></svg>',
                next: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>'
            }
        },
        drawCallback: function() {
            $('.dataTables_paginate').addClass('btn-group');
            $('.dataTables_paginate .paginate_button').addClass('btn btn-ghost btn-sm');
        }
    });

    // 2. Tab Switching Engine
    $('.tab').on('click', function() {
        const tabId = $(this).data('tab');
        
        // Update Tabs UI
        $('.tab').removeClass('active');
        $(this).addClass('active');
        
        // Update Content
        $('.tab-content').fadeOut(100, function() {
            $('.tab-content').removeClass('active');
            $('#' + tabId).addClass('active').fadeIn(200, function() {
                // Ensure DataTable adjusts its columns after being shown
                if (tabId === 'control') {
                    jobsTable.columns.adjust().responsive.recalc();
                }
            });
        });

        // Smart Monitoring Reload
        if (tabId === 'monitor') {
            const frame = document.getElementById('hangfireFrame');
            if (frame && frame.contentWindow) {
                frame.contentWindow.location.reload();
            }
        }
    });

    // Handle window resize for fluid responsiveness
    $(window).on('resize', function() {
        jobsTable.columns.adjust().responsive.recalc();
    });
});

/**
 * Trigger immediate background execution
 */
function triggerNow(jobId) {
    Swal.fire({
        title: 'Background Process Request',
        text: "Are you sure you want to trigger this execution now?",
        icon: 'info',
        showCancelButton: true,
        confirmButtonText: 'Run Now',
        background: 'rgba(18, 18, 21, 0.95)',
        customClass: {
            popup: 'glass-modal border-glass',
            confirmButton: 'btn btn-primary px-4',
            cancelButton: 'btn btn-ghost px-4'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            $.post('/Jobs/Trigger', { jobId: jobId }, function (res) {
                if (res.succeeded) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Enqueued',
                        text: res.message,
                        timer: 1500,
                        showConfirmButton: false,
                        background: 'rgba(18, 18, 21, 0.95)',
                        customClass: { popup: 'glass-modal border-glass' }
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Failure',
                        text: res.message,
                        background: 'rgba(18, 18, 21, 0.95)',
                        customClass: { popup: 'glass-modal border-glass' }
                    });
                }
            });
        }
    });
}

/**
 * Configure Recurring Schedule
 */
function showCreateJobForm() {
    Swal.fire({
        title: 'New Service Configuration',
        html: `
            <div class="swal-form" style="text-align: left; padding: 10px 0;">
                <div class="form-group" style="margin-bottom: 20px;">
                    <label style="display:block; font-size:12px; font-weight:700; color:var(--text-muted); margin-bottom:8px; text-transform:uppercase;">Job Identifier</label>
                    <input id="swal-job-id" class="swal2-input" placeholder="e.g. system-cleanup" style="width:100% !important; margin:0 !important; background:#000 !important; border-color:rgba(255,255,255,0.1) !important;">
                </div>
                <div class="form-group" style="margin-bottom: 20px;">
                    <label style="display:block; font-size:12px; font-weight:700; color:var(--text-muted); margin-bottom:8px; text-transform:uppercase;">Execution Logic</label>
                    <select id="swal-job-type" class="swal2-input" style="width:100% !important; margin:0 !important; background:#000 !important; border-color:rgba(255,255,255,0.1) !important;">
                        <option value="1">Core: Mark Overdue Invoices</option>
                        <option value="2">System: Temporary Files Cleanup</option>
                        <option value="3">Storage: Tenant Data Backup</option>
                    </select>
                </div>
                <div class="form-group">
                    <label style="display:block; font-size:12px; font-weight:700; color:var(--text-muted); margin-bottom:8px; text-transform:uppercase;">Schedule (CRON)</label>
                    <input id="swal-job-cron" class="swal2-input" value="0 0 * * *" style="width:100% !important; margin:0 !important; background:#000 !important; border-color:rgba(255,255,255,0.1) !important;">
                    <div style="margin-top:12px; display:grid; grid-template-columns: 1fr 1fr 1fr; gap:8px;">
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-job-cron').value='* * * * *'">Minute</button>
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-job-cron').value='0 * * * *'">Hourly</button>
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-job-cron').value='0 0 * * *'">Daily</button>
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-job-cron').value='0 0 * * 0'">Weekly</button>
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-job-cron').value='0 0 1 * *'">Monthly</button>
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-job-cron').value='0 0 1 1 *'">Yearly</button>
                    </div>
                </div>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: 'Register Service',
        background: 'rgba(18, 18, 21, 0.95)',
        customClass: {
            popup: 'glass-modal border-glass',
            confirmButton: 'btn btn-primary px-4',
            cancelButton: 'btn btn-ghost px-4'
        },
        preConfirm: () => {
            const id = document.getElementById('swal-job-id').value;
            const type = document.getElementById('swal-job-type').value;
            const cron = document.getElementById('swal-job-cron').value;
            if (!id || !cron) {
                Swal.showValidationMessage('Required fields missing');
                return false;
            }
            return { id, type, cron };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            $.post('/Jobs/CreateCustom', { 
                jobId: result.value.id, 
                jobType: result.value.type, 
                cron: result.value.cron 
            }, function (res) {
                if (res.succeeded) {
                    Swal.fire({
                        icon: 'success', 
                        title: 'Success', 
                        text: res.message,
                        background: 'rgba(18, 18, 21, 0.95)',
                        customClass: { popup: 'glass-modal border-glass' }
                    }).then(() => location.reload());
                } else {
                    Swal.fire({
                        icon: 'error', 
                        title: 'Error', 
                        text: res.message,
                        background: 'rgba(18, 18, 21, 0.95)',
                        customClass: { popup: 'glass-modal border-glass' }
                    });
                }
            });
        }
    });
}

/**
 * Toggle or Remove Schedule
 */
function toggleSchedule(jobId, active) {
    if (active) {
        Swal.fire({
            title: 'Modify Configuration',
            html: `
                <div class="swal-form" style="text-align: left; padding: 10px 0;">
                    <label style="display:block; font-size:12px; font-weight:700; color:var(--text-muted); margin-bottom:8px; text-transform:uppercase;">Update CRON Schedule</label>
                    <input id="swal-input-cron" class="swal2-input" value="0 0 * * *" style="width:100% !important; margin:0 !important; background:#000 !important; border-color:rgba(255,255,255,0.1) !important;">
                    <div style="margin-top:12px; display:grid; grid-template-columns: 1fr 1fr 1fr; gap:8px;">
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-input-cron').value='* * * * *'">Minute</button>
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-input-cron').value='0 * * * *'">Hourly</button>
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-input-cron').value='0 0 * * *'">Daily</button>
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-input-cron').value='0 0 * * 0'">Weekly</button>
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-input-cron').value='0 0 1 * *'">Monthly</button>
                        <button type="button" class="btn btn-secondary btn-xs" onclick="document.getElementById('swal-input-cron').value='0 0 1 1 *'">Yearly</button>
                    </div>
                </div>
            `,
            showCancelButton: true,
            confirmButtonText: 'Update',
            background: 'rgba(18, 18, 21, 0.95)',
            customClass: {
                popup: 'glass-modal border-glass',
                confirmButton: 'btn btn-primary px-4',
                cancelButton: 'btn btn-ghost px-4'
            },
            preConfirm: () => document.getElementById('swal-input-cron').value
        }).then((result) => {
            if (result.isConfirmed) {
                $.post('/Jobs/Schedule', { jobId: jobId, cron: result.value }, function (res) {
                    if (res.succeeded) {
                        Swal.fire({
                            icon: 'success', 
                            title: 'Saved', 
                            text: res.message,
                            background: 'rgba(18, 18, 21, 0.95)',
                            customClass: { popup: 'glass-modal border-glass' }
                        }).then(() => location.reload());
                    } else {
                        Swal.fire({
                            icon: 'error', 
                            title: 'Failed', 
                            text: res.message,
                            background: 'rgba(18, 18, 21, 0.95)',
                            customClass: { popup: 'glass-modal border-glass' }
                        });
                    }
                });
            }
        });
    } else {
        Swal.fire({
            title: 'Danger Zone',
            text: "This will permanently remove the recurring schedule for this task.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: 'var(--status-error)',
            confirmButtonText: 'Delete Schedule',
            background: 'rgba(18, 18, 21, 0.95)',
            customClass: {
                popup: 'glass-modal border-glass',
                confirmButton: 'btn btn-danger px-4',
                cancelButton: 'btn btn-ghost px-4'
            }
        }).then((result) => {
            if (result.isConfirmed) {
                $.post('/Jobs/Remove', { jobId: jobId }, function (res) {
                    if (res.succeeded) {
                         location.reload();
                    } else {
                        Swal.fire({
                            icon: 'error', 
                            title: 'Error', 
                            text: res.message,
                            background: 'rgba(18, 18, 21, 0.95)',
                            customClass: { popup: 'glass-modal border-glass' }
                        });
                    }
                });
            }
        });
    }
}
