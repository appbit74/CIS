// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
    $(document).ready(function() {

        // -------------------------------------------------
        // 1. ดักจับการ Submit Form ทุกฟอร์มในระบบ
        // -------------------------------------------------
        $('form').on('submit', function () {
            var $form = $(this);

            // ตรวจสอบว่า Form ผ่าน Validation หรือไม่ (ถ้าใช้ jQuery Validate)
            if ($form.valid()) {
                // แสดง Loader
                $('#global-loader').css('display', 'flex');

                // หาปุ่ม Submit ในฟอร์มแล้ว Disable เพื่อกันกดซ้ำ
                var $btn = $form.find('button[type="submit"], input[type="submit"]');
                $btn.prop('disabled', true);

                // (Optional) เปลี่ยนข้อความปุ่ม
                // $btn.data('original-text', $btn.html());
                // $btn.html('<i class="fas fa-spinner fa-spin"></i> กำลังประมวลผล...');
            }
        });

    // -------------------------------------------------
    // 2. ดักจับการกด Link (เปลี่ยนหน้า)
    // -------------------------------------------------
    $('a').on('click', function(e) {
            var href = $(this).attr('href');
    var target = $(this).attr('target');

    // เงื่อนไขที่ไม่ต้องแสดง Loader
    if (href &&
    href !== '#' &&
    !href.startsWith('javascript') &&
    target !== '_blank' &&
    !$(this).hasClass('no-loader')) // ใส่ class 'no-loader' ถ้าไม่อยากให้หมุน
    {
        $('#global-loader').css('display', 'flex');
            }
        });
    });

    // -------------------------------------------------
    // 3. แก้ปัญหาปุ่ม Back (ถ้ากด Back กลับมา Loader ต้องหายไป)
    // -------------------------------------------------
    window.onpageshow = function(event) {
        if (event.persisted) {
        $('#global-loader').hide();
    // Enable ปุ่ม Submit กลับคืนมา
    $('button[type="submit"], input[type="submit"]').prop('disabled', false);
        }
    };
