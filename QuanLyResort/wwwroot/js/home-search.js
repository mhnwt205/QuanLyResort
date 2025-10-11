$(document).ready(function() {
    function sel(id, name) {
        var byId = $('#' + id);
        if (byId.length) return byId;
        var byName = $('input[name="' + name + '"]');
        if (byName.length) return byName;
        // class fallback
        if (name === 'CheckInDate') return $('.checkin_date');
        if (name === 'CheckOutDate') return $('.checkout_date');
        return $();
    }

    var $ci = sel('CheckInDate', 'CheckInDate');
    var $co = sel('CheckOutDate', 'CheckOutDate');

    // Debug: Kiểm tra xem date inputs có tồn tại không
    console.log('CheckInDate element:', $ci.length);
    console.log('CheckOutDate element:', $co.length);
    // Debug helper to inspect validity
    function logValidity($el, label) {
        if (!$el || !$el.length) return;
        var el = $el[0];
        var v = el.validity || {};
        console.log('[validity]', label, {
            value: $el.val(),
            pattern: $el.attr('pattern'),
            type: $el.attr('type'),
            dataVal: $el.attr('data-val'),
            dataValDate: $el.attr('data-val-date'),
            validity: {
                valid: v.valid,
                badInput: v.badInput,
                patternMismatch: v.patternMismatch,
                typeMismatch: v.typeMismatch,
                valueMissing: v.valueMissing
            }
        });
    }
    
    // Set ngày mặc định trước
    var today = new Date();
    var tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    var dayAfter = new Date(today);
    dayAfter.setDate(dayAfter.getDate() + 2);
    
    var tomorrowStr = tomorrow.toISOString().split('T')[0];
    var dayAfterStr = dayAfter.toISOString().split('T')[0];
    
    console.log('Setting default dates:', tomorrowStr, dayAfterStr);
    
    if (!$ci.val()) {
        $ci.val(tomorrowStr);
        console.log('Set CheckInDate to:', tomorrowStr);
    }
    if (!$co.val()) {
        $co.val(dayAfterStr);
        console.log('Set CheckOutDate to:', dayAfterStr);
    }
    
    // Đảm bảo date inputs có thể click được
    $ci.add($co).prop('disabled', false).prop('readonly', false);
    
    // Thêm event listener để debug
    $ci.add($co).on('click focus', function() {
        console.log('Date input clicked/focused:', $(this).attr('id'));
    });
    
    function normalizeToYMD(inputStr) {
        if (!inputStr) return '';
        // If already yyyy-mm-dd
        if (/^\d{4}-\d{2}-\d{2}$/.test(inputStr)) return inputStr;
        // Accept dd/MM/yyyy or MM/dd/yyyy or yyyy/MM/dd
        var m = inputStr.match(/^([0-3]?\d)[\/\-]([0-1]?\d)[\/\-](\d{4})$/); // dd/MM/yyyy
        if (m) {
            var d1 = m[1].padStart(2,'0');
            var m1 = m[2].padStart(2,'0');
            var y1 = m[3];
            return y1 + '-' + m1 + '-' + d1;
        }
        m = inputStr.match(/^(\d{4})[\/\-]([0-1]?\d)[\/\-]([0-3]?\d)$/); // yyyy/MM/dd
        if (m) {
            var y2 = m[1];
            var m2 = m[2].padStart(2,'0');
            var d2 = m[3].padStart(2,'0');
            return y2 + '-' + m2 + '-' + d2;
        }
        // Fallback native Date
        var d = new Date(inputStr);
        if (!isNaN(d.getTime())) {
            var y = d.getFullYear();
            var m = String(d.getMonth() + 1).padStart(2, '0');
            var day = String(d.getDate()).padStart(2, '0');
            return y + '-' + m + '-' + day;
        }
        return inputStr; // leave as is if unknown
    }

    // Init Bootstrap Datepicker if available
    if ($.fn.datepicker && ($ci.length || $co.length)) {
        $ci.add($co).datepicker({
            format: 'yyyy-mm-dd',
            autoclose: true,
            todayHighlight: true
        }).on('changeDate change', function(e) {
            var val = $(this).val();
            var normalized = normalizeToYMD(val);
            $(this).val(normalized);
            $ci.add($co).trigger('validateDates');
        });

        // Mở lịch ngay khi focus/click icon
        $ci.add($co).on('focus', function() {
            $(this).datepicker('show');
        });
        $('.search-icon').on('click', function() {
            var $input = $(this).siblings('input');
            if ($input.length) {
                $input.focus();
                $input.datepicker('show');
            }
        });
    }

    // Validation ngày + normalize khi change/input
    $ci.add($co).on('change input validateDates', function() {
        var ci = normalizeToYMD($ci.val());
        var co = normalizeToYMD($co.val());
        $ci.val(ci);
        $co.val(co);

        var checkinDate = ci ? new Date(ci) : null;
        var checkoutDate = co ? new Date(co) : null;

        if (checkinDate && checkoutDate && !isNaN(checkinDate) && !isNaN(checkoutDate)) {
            if (checkoutDate <= checkinDate) {
                alert('Ngày trả phòng phải sau ngày nhận phòng');
            $co.val('');
                return;
            }
        }

        // Tự động set ngày checkout nếu chưa có
        if (ci && !co) {
            var nextDay = new Date(ci);
            nextDay.setDate(nextDay.getDate() + 1);
            var nextStr = nextDay.toISOString().split('T')[0];
            $co.val(nextStr);
        }
    });
    
    // jQuery validate: custom rule date chấp nhận yyyy-MM-dd
    if (window.jQuery && $.validator && $.validator.methods && $.validator.methods.date) {
        var original = $.validator.methods.date;
        $.validator.methods.date = function(value, element) {
            if (!value) return true;
            var ok = /^\d{4}-\d{2}-\d{2}$/.test(normalizeToYMD(value));
            if (!ok) {
                console.warn('[jquery-validate] date rejected', { id: $(element).attr('id'), value });
            }
            return ok;
        };
    }
    
    // Hiệu ứng loading khi submit
    $('#searchForm').submit(function(e) {
        // Force normalize before submit
        $ci.val(normalizeToYMD($ci.val()));
        $co.val(normalizeToYMD($co.val()));
        logValidity($ci, 'CheckInDate-beforeSubmit');
        logValidity($co, 'CheckOutDate-beforeSubmit');
        console.log('[home-search] submit triggered');
        console.log('values =>', {
            checkIn: sel('CheckInDate','CheckInDate').val(),
            checkOut: sel('CheckOutDate','CheckOutDate').val(),
            guests: $('#GuestCount').val(),
            roomType: $('#RoomTypeId').val()
        });
        var submitBtn = $(this).find('button[type="submit"]');
        var btnText = submitBtn.find('.btn-search-text');
        var originalText = btnText.text();
        
        // Disable button và thay đổi text
        submitBtn.prop('disabled', true);
        btnText.text('Đang tìm kiếm...');
        
        // Thêm loading animation
        submitBtn.addClass('loading');
        
        // Re-enable button after 5 seconds (fallback)
        setTimeout(function() {
            submitBtn.prop('disabled', false);
            btnText.text(originalText);
            submitBtn.removeClass('loading');
        }, 5000);
        
        // Form sẽ submit và chuyển hướng đến trang Rooms
        return true;
    });
    
    // Hiệu ứng hover cho quick search buttons
    $('.quick-search-btn').hover(
        function() {
            $(this).addClass('btn-primary').removeClass('btn-outline-primary');
        },
        function() {
            $(this).addClass('btn-outline-primary').removeClass('btn-primary');
        }
    );
    
    // Xử lý tìm kiếm nhanh - chỉ có 1 event handler
    $('.quick-search-btn').click(function(e) {
        e.preventDefault();
        
        // Điền form
        var checkinDate = $(this).data('checkin');
        var checkoutDate = $(this).data('checkout');
        var guests = $(this).data('guests');
        var roomType = $(this).data('roomtype');
        
        $('#CheckInDate').val(checkinDate);
        $('#CheckOutDate').val(checkoutDate);
        $('#GuestCount').val(guests);
        $('#RoomTypeId').val(roomType);
        
        // Scroll to form
        $('html, body').animate({
            scrollTop: $('#searchForm').offset().top - 100
        }, 500);
        
        // Highlight form
        $('#searchForm').addClass('highlight-form');
        setTimeout(function() {
            $('#searchForm').removeClass('highlight-form');
        }, 2000);
        
        // Submit after animation
        setTimeout(function() {
            $('#searchForm').submit();
        }, 1000);
    });
});
