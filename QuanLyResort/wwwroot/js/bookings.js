/**
 * Bookings Management JavaScript
 * Handles DataTables initialization, SignalR Hub connection, and real-time updates
 */

// Global variables
let bookingsTable;
let hotelHub;

$(document).ready(function() {
    initializeBookings();
    initializeSignalR();
    initializeEventHandlers();
});

/**
 * Initialize bookings functionality
 */
function initializeBookings() {
    // Initialize DataTables for bookings table
    if ($('#bookingsTable').length) {
        bookingsTable = $('#bookingsTable').DataTable({
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

    // Initialize booking form validation and calculations
    initializeBookingForm();
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
    connection.start().then(function() {
        console.log("SignalR Hub connected");
        
        // Join admin group
        connection.invoke("JoinGroup", "Admin");
        
        // Store connection globally
        hotelHub = connection;
        
    }).catch(function(err) {
        console.error("SignalR Hub connection failed: " + err.toString());
        showNotification("Kết nối real-time thất bại", "warning");
    });

    // Handle connection events
    connection.onclose(function() {
        console.log("SignalR Hub disconnected");
        showNotification("Mất kết nối real-time", "warning");
    });

    connection.onreconnecting(function() {
        console.log("SignalR Hub reconnecting...");
        showNotification("Đang kết nối lại...", "info");
    });

    connection.onreconnected(function() {
        console.log("SignalR Hub reconnected");
        showNotification("Đã kết nối lại", "success");
        connection.invoke("JoinGroup", "Admin");
    });

    // Register SignalR event handlers
    registerSignalREventHandlers(connection);
}

/**
 * Register SignalR event handlers
 */
function registerSignalREventHandlers(connection) {
    // Handle booking updates
    connection.on("BookingUpdated", function(bookingId) {
        console.log("Booking updated:", bookingId);
        refreshBookingRow(bookingId);
        showNotification("Đặt phòng đã được cập nhật", "info");
    });

    // Handle new bookings
    connection.on("NewBooking", function(bookingId, bookingCode) {
        console.log("New booking:", bookingId, bookingCode);
        refreshBookingsTable();
        showNotification("Có đặt phòng mới: " + bookingCode, "success");
    });

    // Handle room status changes
    connection.on("RoomStatusChanged", function(roomId, status) {
        console.log("Room status changed:", roomId, status);
        updateRoomStatusDisplay(roomId, status);
    });

    // Handle check-in events
    connection.on("CheckIn", function(bookingId, roomNumber) {
        console.log("Check-in:", bookingId, roomNumber);
        refreshBookingRow(bookingId);
        showNotification("Khách đã nhận phòng " + roomNumber, "primary");
    });

    // Handle check-out events
    connection.on("CheckOut", function(bookingId, roomNumber) {
        console.log("Check-out:", bookingId, roomNumber);
        refreshBookingRow(bookingId);
        showNotification("Khách đã trả phòng " + roomNumber, "info");
    });

    // Handle dashboard updates
    connection.on("DashboardUpdate", function() {
        console.log("Dashboard update received");
        // Trigger dashboard refresh if on dashboard page
        if (window.refreshDashboard) {
            window.refreshDashboard();
        }
    });
}

/**
 * Initialize event handlers
 */
function initializeEventHandlers() {
    // Handle booking form submissions
    $(document).on('submit', '#bookingForm', function(e) {
        const form = $(this);
        const submitBtn = form.find('button[type="submit"]');
        
        // Disable submit button to prevent double submission
        submitBtn.prop('disabled', true);
        
        // Re-enable after 3 seconds
        setTimeout(function() {
            submitBtn.prop('disabled', false);
        }, 3000);
    });

    // Handle booking action confirmations
    $(document).on('click', '[data-booking-action]', function(e) {
        const action = $(this).data('booking-action');
        const bookingId = $(this).data('booking-id');
        
        if (confirm(getActionConfirmationMessage(action))) {
            // Action will be handled by form submission
            return true;
        }
        
        e.preventDefault();
        return false;
    });

    // Handle room availability checks
    $(document).on('change', '#roomSelect, #CheckInDate, #CheckOutDate', function() {
        checkRoomAvailability();
    });
}

/**
 * Initialize booking form functionality
 */
function initializeBookingForm() {
    // Set minimum date to today
    const today = new Date().toISOString().split('T')[0];
    $('#CheckInDate, #CheckOutDate').attr('min', today);

    // Calculate total amount when room or dates change
    $(document).on('change', '#roomSelect, #CheckInDate, #CheckOutDate', function() {
        calculateTotalAmount();
    });

    // Set check-out date to next day when check-in date changes
    $('#CheckInDate').on('change', function() {
        const checkInDate = new Date($(this).val());
        const nextDay = new Date(checkInDate);
        nextDay.setDate(nextDay.getDate() + 1);
        $('#CheckOutDate').val(nextDay.toISOString().split('T')[0]);
        calculateTotalAmount();
    });

    // Initial calculation
    calculateTotalAmount();
}

/**
 * Calculate total amount for booking
 */
function calculateTotalAmount() {
    const roomId = $('#roomSelect').val();
    const checkInDate = $('#CheckInDate').val();
    const checkOutDate = $('#CheckOutDate').val();
    
    if (roomId && checkInDate && checkOutDate) {
        const checkIn = new Date(checkInDate);
        const checkOut = new Date(checkOutDate);
        const nights = Math.ceil((checkOut - checkIn) / (1000 * 60 * 60 * 24));
        
        if (nights > 0) {
            const price = parseFloat($('#roomSelect option:selected').data('price'));
            if (price) {
                const total = price * nights;
                $('#totalAmount').val(total.toLocaleString('vi-VN'));
            }
        } else {
            $('#totalAmount').val('');
        }
    } else {
        $('#totalAmount').val('');
    }
}

/**
 * Check room availability
 */
function checkRoomAvailability() {
    const roomId = $('#roomSelect').val();
    const checkInDate = $('#CheckInDate').val();
    const checkOutDate = $('#CheckOutDate').val();
    
    if (roomId && checkInDate && checkOutDate) {
        $.get('/Admin/Bookings/IsRoomAvailable', {
            roomId: roomId,
            checkIn: checkInDate,
            checkOut: checkOutDate
        })
        .done(function(data) {
            if (!data.available) {
                showNotification('Phòng không có sẵn trong khoảng thời gian này', 'warning');
            }
        })
        .fail(function() {
            console.error('Failed to check room availability');
        });
    }
}

/**
 * Refresh a specific booking row in the table
 */
function refreshBookingRow(bookingId) {
    if (bookingsTable) {
        $.get('/Admin/Bookings/GetBookingDetails', { id: bookingId })
            .done(function(data) {
                if (data.success) {
                    // Update the specific row
                    const row = bookingsTable.row('[data-booking-id="' + bookingId + '"]');
                    if (row.length) {
                        // Refresh the entire table for simplicity
                        refreshBookingsTable();
                    }
                }
            })
            .fail(function() {
                console.error('Failed to refresh booking row');
            });
    }
}

/**
 * Refresh the entire bookings table
 */
function refreshBookingsTable() {
    if (bookingsTable) {
        bookingsTable.ajax.reload(null, false); // false = stay on current page
    } else {
        // Reload the page if DataTable is not initialized
        location.reload();
    }
}

/**
 * Update room status display
 */
function updateRoomStatusDisplay(roomId, status) {
    // Update room status badges or indicators
    $(`.room-status[data-room-id="${roomId}"]`).removeClass()
        .addClass(`badge room-status ${getStatusBadgeClass(status)}`)
        .text(getStatusText(status));
}

/**
 * Get status badge class
 */
function getStatusBadgeClass(status) {
    switch (status) {
        case 'available': return 'bg-success';
        case 'booked': return 'bg-warning';
        case 'occupied': return 'bg-primary';
        case 'cleaning': return 'bg-info';
        case 'maintenance': return 'bg-danger';
        default: return 'bg-secondary';
    }
}

/**
 * Get status text
 */
function getStatusText(status) {
    switch (status) {
        case 'available': return 'Có sẵn';
        case 'booked': return 'Đã đặt';
        case 'occupied': return 'Đang sử dụng';
        case 'cleaning': return 'Đang dọn dẹp';
        case 'maintenance': return 'Bảo trì';
        default: return status;
    }
}

/**
 * Get action confirmation message
 */
function getActionConfirmationMessage(action) {
    switch (action) {
        case 'confirm': return 'Xác nhận đặt phòng này?';
        case 'checkin': return 'Thực hiện check-in?';
        case 'checkout': return 'Thực hiện check-out?';
        case 'cancel': return 'Hủy đặt phòng này?';
        case 'delete': return 'Xóa đặt phòng này?';
        default: return 'Thực hiện thao tác này?';
    }
}

/**
 * Show notification
 */
function showNotification(message, type = 'info') {
    // Remove existing notifications
    $('.toast-container .toast').remove();
    
    // Create toast notification
    const toastHtml = `
        <div class="toast align-items-center text-white bg-${type} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;
    
    // Add to container
    $('.toast-container').append(toastHtml);
    
    // Show toast
    const toast = new bootstrap.Toast($('.toast-container .toast').last()[0]);
    toast.show();
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

/**
 * Calculate nights between two dates
 */
function calculateNights(checkInDate, checkOutDate) {
    const checkIn = new Date(checkInDate);
    const checkOut = new Date(checkOutDate);
    const timeDiff = checkOut.getTime() - checkIn.getTime();
    return Math.ceil(timeDiff / (1000 * 3600 * 24));
}

// Export functions for global access
window.refreshBookingsTable = refreshBookingsTable;
window.refreshBookingRow = refreshBookingRow;
window.showNotification = showNotification;
window.formatCurrency = formatCurrency;
window.formatDate = formatDate;
