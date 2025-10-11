/**
 * Invoices & Payments Management JavaScript
 * Handles DataTables initialization, SignalR Hub connection, and real-time updates
 */

// Global variables
let invoicesTable;
let paymentsTable;
let hotelHub;

$(document).ready(function() {
    initializeInvoicesPayments();
    initializeSignalR();
    initializeEventHandlers();
});

/**
 * Initialize invoices and payments functionality
 */
function initializeInvoicesPayments() {
    // Initialize DataTables for invoices table
    if ($('#invoicesTable').length) {
        invoicesTable = $('#invoicesTable').DataTable({
            "language": {
                "url": "//cdn.datatables.net/plug-ins/1.10.24/i18n/Vietnamese.json"
            },
            "order": [[0, "desc"]],
            "pageLength": 25,
            "responsive": true,
            "columnDefs": [
                { "orderable": false, "targets": 7 } // Disable sorting on actions column
            ],
            "drawCallback": function(settings) {
                // Re-initialize tooltips after table redraw
                $('[data-bs-toggle="tooltip"]').tooltip();
            }
        });
    }

    // Initialize DataTables for payments table
    if ($('#paymentsTable').length) {
        paymentsTable = $('#paymentsTable').DataTable({
            "language": {
                "url": "//cdn.datatables.net/plug-ins/1.10.24/i18n/Vietnamese.json"
            },
            "order": [[0, "desc"]],
            "pageLength": 25,
            "responsive": true,
            "columnDefs": [
                { "orderable": false, "targets": 7 } // Disable sorting on actions column
            ],
            "drawCallback": function(settings) {
                // Re-initialize tooltips after table redraw
                $('[data-bs-toggle="tooltip"]').tooltip();
            }
        });
    }

    // Initialize forms
    initializeInvoiceForm();
    initializePaymentForm();
}

/**
 * Initialize SignalR Hub connection
 */
function initializeSignalR() {
    // Create SignalR connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hotelHub")
        .withAutomaticReconnect()
        .build();

    // Start connection
    connection.start()
        .then(function () {
            console.log("SignalR Connected for Invoices/Payments.");
            // Join Admin group for notifications
            return connection.invoke("JoinGroup", "Admin");
        })
        .catch(function (err) {
            console.error("SignalR connection error: " + err.toString());
        });

    // Invoice events
    connection.on("InvoiceGenerated", function (invoiceId, invoiceNumber) {
        showNotification("Hóa đơn mới", `Hóa đơn #${invoiceNumber} đã được tạo.`, "success");
        refreshInvoicesTable();
    });

    connection.on("InvoiceUpdated", function (invoiceId, invoiceNumber, status) {
        showNotification("Cập nhật hóa đơn", `Hóa đơn #${invoiceNumber} đã được cập nhật. Trạng thái: ${getStatusText(status)}.`, "info");
        refreshInvoiceRow(invoiceId);
    });

    connection.on("InvoiceApproved", function (invoiceId, invoiceNumber) {
        showNotification("Hóa đơn được duyệt", `Hóa đơn #${invoiceNumber} đã được duyệt.`, "success");
        refreshInvoiceRow(invoiceId);
    });

    connection.on("InvoiceCancelled", function (invoiceId, invoiceNumber) {
        showNotification("Hóa đơn bị hủy", `Hóa đơn #${invoiceNumber} đã bị hủy.`, "warning");
        refreshInvoiceRow(invoiceId);
    });

    // Payment events
    connection.on("PaymentProcessed", function (paymentId, paymentNumber, amount) {
        showNotification("Thanh toán mới", `Thanh toán #${paymentNumber} với số tiền ${formatCurrency(amount)} đã được xử lý.`, "success");
        refreshPaymentsTable();
    });

    connection.on("PaymentRefunded", function (paymentId, paymentNumber, refundAmount) {
        showNotification("Hoàn tiền", `Hoàn tiền #${paymentNumber} với số tiền ${formatCurrency(refundAmount)} đã được xử lý.`, "warning");
        refreshPaymentsTable();
    });

    // Store connection globally
    hotelHub = connection;
}

/**
 * Initialize event handlers
 */
function initializeEventHandlers() {
    // Invoice form handlers
    $('#invoiceSelect').on('change', function() {
        loadInvoiceDetails($(this).val());
    });

    // Payment form handlers
    $('#paymentInvoiceSelect').on('change', function() {
        loadPaymentInvoiceDetails($(this).val());
    });

    // Auto-calculate totals
    $('input[name="Subtotal"], input[name="TaxAmount"], input[name="DiscountAmount"]').on('input', calculateInvoiceTotal);
    $('input[name="Amount"]').on('input', validatePaymentAmount);
}

/**
 * Initialize invoice form
 */
function initializeInvoiceForm() {
    // Set default dates
    const today = new Date();
    $('input[name="InvoiceDate"]').val(today.toISOString().split('T')[0]);
    
    const dueDate = new Date(today);
    dueDate.setDate(dueDate.getDate() + 7);
    $('input[name="DueDate"]').val(dueDate.toISOString().split('T')[0]);

    // Auto-calculate total
    calculateInvoiceTotal();
}

/**
 * Initialize payment form
 */
function initializePaymentForm() {
    // Set default date
    const today = new Date();
    $('input[name="PaymentDate"]').val(today.toISOString().split('T')[0]);
}

/**
 * Calculate invoice total
 */
function calculateInvoiceTotal() {
    const subtotal = parseFloat($('input[name="Subtotal"]').val()) || 0;
    const tax = parseFloat($('input[name="TaxAmount"]').val()) || 0;
    const discount = parseFloat($('input[name="DiscountAmount"]').val()) || 0;
    const total = subtotal + tax - discount;
    
    $('input[name="TotalAmount"]').val(total.toFixed(2));
}

/**
 * Load invoice details
 */
function loadInvoiceDetails(invoiceId) {
    if (!invoiceId) {
        $('#invoice-info').html('<p class="text-muted">Chọn hóa đơn để xem thông tin chi tiết</p>');
        return;
    }

    // Show loading
    $('#invoice-info').html('<p class="text-info">Đang tải thông tin hóa đơn...</p>');

    // In a real application, make AJAX call to get invoice details
    // For now, we'll simulate with static data
    setTimeout(function() {
        $('#invoice-info').html(`
            <table class="table table-borderless">
                <tr>
                    <td class="fw-semibold">Khách hàng:</td>
                    <td>Đang tải...</td>
                </tr>
                <tr>
                    <td class="fw-semibold">Tổng tiền:</td>
                    <td>Đang tải...</td>
                </tr>
                <tr>
                    <td class="fw-semibold">Trạng thái:</td>
                    <td>Đang tải...</td>
                </tr>
            </table>
        `);
    }, 500);
}

/**
 * Load payment invoice details
 */
function loadPaymentInvoiceDetails(invoiceId) {
    if (!invoiceId) {
        $('#payment-invoice-info').html('<p class="text-muted">Chọn hóa đơn để xem thông tin chi tiết</p>');
        return;
    }

    // Show loading
    $('#payment-invoice-info').html('<p class="text-info">Đang tải thông tin hóa đơn...</p>');

    // In a real application, make AJAX call to get invoice details
    setTimeout(function() {
        $('#payment-invoice-info').html(`
            <table class="table table-borderless">
                <tr>
                    <td class="fw-semibold">Khách hàng:</td>
                    <td>Đang tải...</td>
                </tr>
                <tr>
                    <td class="fw-semibold">Tổng tiền:</td>
                    <td>Đang tải...</td>
                </tr>
                <tr>
                    <td class="fw-semibold">Đã thanh toán:</td>
                    <td>Đang tải...</td>
                </tr>
                <tr>
                    <td class="fw-semibold">Còn lại:</td>
                    <td>Đang tải...</td>
                </tr>
            </table>
        `);
    }, 500);
}

/**
 * Validate payment amount
 */
function validatePaymentAmount() {
    const amount = parseFloat($('input[name="Amount"]').val()) || 0;
    const invoiceSelect = $('#paymentInvoiceSelect');
    
    if (invoiceSelect.length && invoiceSelect.val()) {
        const selectedOption = invoiceSelect.find('option:selected');
        const total = parseFloat(selectedOption.data('total')) || 0;
        const paid = parseFloat(selectedOption.data('paid')) || 0;
        const remaining = total - paid;

        if (amount > remaining) {
            $('input[name="Amount"]')[0].setCustomValidity('Số tiền thanh toán không được vượt quá số tiền còn lại');
        } else if (amount <= 0) {
            $('input[name="Amount"]')[0].setCustomValidity('Số tiền thanh toán phải lớn hơn 0');
        } else {
            $('input[name="Amount"]')[0].setCustomValidity('');
        }
    }
}

/**
 * Refresh invoices table
 */
function refreshInvoicesTable() {
    if (invoicesTable) {
        invoicesTable.ajax.reload();
    } else {
        location.reload();
    }
}

/**
 * Refresh payments table
 */
function refreshPaymentsTable() {
    if (paymentsTable) {
        paymentsTable.ajax.reload();
    } else {
        location.reload();
    }
}

/**
 * Refresh specific invoice row
 */
function refreshInvoiceRow(invoiceId) {
    if (invoicesTable) {
        // Find and refresh specific row
        const row = invoicesTable.row(`tr[data-invoice-id="${invoiceId}"]`);
        if (row.length) {
            // In a real application, fetch updated data via AJAX
            row.invalidate().draw();
        }
    } else {
        refreshInvoicesTable();
    }
}

/**
 * Refresh specific payment row
 */
function refreshPaymentRow(paymentId) {
    if (paymentsTable) {
        // Find and refresh specific row
        const row = paymentsTable.row(`tr[data-payment-id="${paymentId}"]`);
        if (row.length) {
            // In a real application, fetch updated data via AJAX
            row.invalidate().draw();
        }
    } else {
        refreshPaymentsTable();
    }
}

/**
 * Show notification toast
 */
function showNotification(title, message, type = "info") {
    // Create toast container if it doesn't exist
    if ($('.toast-container').length === 0) {
        $('body').append('<div class="toast-container position-fixed top-0 end-0 p-3"></div>');
    }

    const toastId = 'toast-' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <strong>${title}</strong><br>${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;
    
    // Add to container
    $('.toast-container').append(toastHtml);
    
    // Show toast
    const toast = new bootstrap.Toast($('.toast-container .toast').last()[0]);
    toast.show();

    // Auto remove after 5 seconds
    setTimeout(function() {
        $(`#${toastId}`).remove();
    }, 5000);
}

/**
 * Get status text in Vietnamese
 */
function getStatusText(status) {
    const statusMap = {
        'draft': 'Nháp',
        'approved': 'Đã duyệt',
        'paid': 'Đã thanh toán',
        'partial': 'Thanh toán một phần',
        'cancelled': 'Đã hủy'
    };
    return statusMap[status] || status;
}

/**
 * Format currency
 */
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN').format(amount) + ' VNĐ';
}

/**
 * Format date
 */
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN');
}

// Export functions for global access
window.refreshInvoicesTable = refreshInvoicesTable;
window.refreshPaymentsTable = refreshPaymentsTable;
window.refreshInvoiceRow = refreshInvoiceRow;
window.refreshPaymentRow = refreshPaymentRow;
window.showNotification = showNotification;
window.formatCurrency = formatCurrency;
window.formatDate = formatDate;
