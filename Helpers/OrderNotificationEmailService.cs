using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using tieudaobadminton.Models;

namespace tieudaobadminton.Helpers
{
    public class OrderNotificationEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<OrderNotificationEmailService> _logger;

        public OrderNotificationEmailService(IConfiguration config, ILogger<OrderNotificationEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendOrderNotificationsAsync(
            Order order,
            List<CartItem> items,
            string customerEmail,
            string paymentMethod,
            decimal shippingFee)
        {
            var host = _config["SmtpSettings:Host"] ?? "smtp.gmail.com";
            var port = int.TryParse(_config["SmtpSettings:Port"], out var p) ? p : 587;
            var enableSsl = !bool.TryParse(_config["SmtpSettings:EnableSsl"], out var ssl) || ssl;
            var senderName = _config["SmtpSettings:SenderName"] ?? "Tiêu Đao Badminton Pro";
            var senderEmail = _config["SmtpSettings:SenderEmail"] ?? "";
            var appPassword = _config["SmtpSettings:AppPassword"] ?? "";
            var adminEmail = _config["SmtpSettings:AdminEmail"] ?? senderEmail;

            // Kiểm tra cấu hình SMTP đã được điền hay chưa
            if (string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(appPassword) ||
                senderEmail == "your-email@gmail.com" ||
                appPassword == "your-app-password")
            {
                _logger.LogWarning("⚠️ [SMTP Notice] Chưa cấu hình tài khoản SenderEmail / AppPassword thực tế trong appsettings.json. Bỏ qua gửi mail thực tế cho đơn #DH{OrderId:D5}.", order.Id);
                return false;
            }

            bool anySent = false;
            var orderCode = $"DH{order.Id:D5}";
            var paymentDisplay = paymentMethod == "BankTransfer"
                ? "Chuyển khoản Ngân hàng (VietQR)"
                : "Thanh toán khi nhận hàng (COD)";

            try
            {
                using var smtpClient = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000
                };

                // 1. GỬI EMAIL XÁC NHẬN ĐẶT HÀNG THÀNH CÔNG VỀ CHO USER (KHÁCH HÀNG)
                if (!string.IsNullOrWhiteSpace(customerEmail))
                {
                    var userSubject = $"🏸 [Tiêu Đao Badminton] Xác nhận đặt hàng thành công #{orderCode}";
                    var userBody = BuildCustomerEmailHtml(order, items, orderCode, paymentDisplay, shippingFee);

                    using var userMail = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName, Encoding.UTF8),
                        Subject = userSubject,
                        SubjectEncoding = Encoding.UTF8,
                        Body = userBody,
                        BodyEncoding = Encoding.UTF8,
                        IsBodyHtml = true
                    };
                    userMail.To.Add(new MailAddress(customerEmail.Trim(), order.FullName));

                    await smtpClient.SendMailAsync(userMail);
                    _logger.LogInformation("✅ Đã gửi email xác nhận đơn hàng #{OrderCode} tới khách hàng: {CustomerEmail}", orderCode, customerEmail);
                    anySent = true;
                }

                // 2. GỬI EMAIL THÔNG BÁO CÓ ĐƠN HÀNG MỚI VỀ CHO ADMIN
                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    var adminSubject = $"🔔 [ĐƠN HÀNG MỚI #{orderCode}] Khách hàng {order.FullName} - {order.Total:n0} đ";
                    var adminBody = BuildAdminEmailHtml(order, items, orderCode, customerEmail, paymentDisplay, shippingFee);

                    using var adminMail = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName, Encoding.UTF8),
                        Subject = adminSubject,
                        SubjectEncoding = Encoding.UTF8,
                        Body = adminBody,
                        BodyEncoding = Encoding.UTF8,
                        IsBodyHtml = true
                    };
                    adminMail.To.Add(new MailAddress(adminEmail.Trim(), "Quản trị viên Tiêu Đao"));

                    await smtpClient.SendMailAsync(adminMail);
                    _logger.LogInformation("✅ Đã gửi email thông báo đơn hàng mới #{OrderCode} tới Admin: {AdminEmail}", orderCode, adminEmail);
                    anySent = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi gửi email SMTP cho đơn hàng #{OrderCode}: {Message}", orderCode, ex.Message);
            }

            return anySent;
        }

        private static string BuildItemsTableRowsHtml(List<CartItem> items)
        {
            var sb = new StringBuilder();
            foreach (var item in items)
            {
                var variantInfo = new List<string>();
                if (!string.IsNullOrWhiteSpace(item.Color) && item.Color != "-")
                    variantInfo.Add($"Màu: {WebUtility.HtmlEncode(item.Color)}");
                if (!string.IsNullOrWhiteSpace(item.Size) && item.Size != "-")
                    variantInfo.Add($"Size: {WebUtility.HtmlEncode(item.Size)}");

                var variantText = variantInfo.Count > 0 ? string.Join(" | ", variantInfo) : "Tiêu chuẩn";
                var lineTotal = item.UnitPrice * item.Quantity;

                sb.Append($@"
                <tr>
                    <td style='padding: 10px 12px; border-bottom: 1px solid #e2e8f0; font-weight: 600; color: #0f172a;'>{WebUtility.HtmlEncode(item.ProductName)}</td>
                    <td style='padding: 10px 12px; border-bottom: 1px solid #e2e8f0; color: #64748b; font-size: 13px;'>{variantText}</td>
                    <td style='padding: 10px 12px; border-bottom: 1px solid #e2e8f0; text-align: center; font-weight: 600;'>x{item.Quantity}</td>
                    <td style='padding: 10px 12px; border-bottom: 1px solid #e2e8f0; text-align: right;'>{item.UnitPrice:n0} đ</td>
                    <td style='padding: 10px 12px; border-bottom: 1px solid #e2e8f0; text-align: right; font-weight: 700; color: #0f172a;'>{lineTotal:n0} đ</td>
                </tr>");
            }
            return sb.ToString();
        }

        private static string BuildCustomerEmailHtml(
            Order order,
            List<CartItem> items,
            string orderCode,
            string paymentDisplay,
            decimal shippingFee)
        {
            var rowsHtml = BuildItemsTableRowsHtml(items);
            var shippingText = shippingFee == 0 ? "Miễn phí (0 đ)" : $"{shippingFee:n0} đ";

            return $@"
<!DOCTYPE html>
<html lang='vi'>
<body style='margin:0; padding:0; background-color:#f1f5f9; font-family: Arial, Helvetica, sans-serif; color:#1e293b;'>
    <div style='max-width:640px; margin:24px auto; background:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 8px 24px rgba(15,23,42,0.08); border:1px solid #e2e8f0;'>
        <!-- Header -->
        <div style='background:linear-gradient(135deg,#0f172a 0%,#1e3a8a 100%); padding:28px 24px; text-align:center; color:#ffffff;'>
            <div style='font-size:22px; font-weight:800; letter-spacing:0.5px;'>🏸 TIÊU ĐAO BADMINTON PRO</div>
            <div style='font-size:14px; color:#cbd5e1; margin-top:6px;'>Hệ thống Vợt, Giày & Dịch vụ Căng Cước Chuẩn Quốc Tế</div>
        </div>

        <!-- Body -->
        <div style='padding:28px 24px;'>
            <div style='text-align:center; margin-bottom:24px;'>
                <div style='display:inline-block; background:#dcfce7; color:#15803d; font-weight:800; padding:6px 16px; border-radius:999px; font-size:13px;'>
                    ✓ ĐẶT HÀNG THÀNH CÔNG
                </div>
                <h2 style='margin:12px 0 6px; color:#0f172a;'>Cảm ơn bạn đã đặt hàng, {WebUtility.HtmlEncode(order.FullName)}!</h2>
                <p style='margin:0; color:#64748b; font-size:14px;'>
                    Đơn hàng <strong>#{orderCode}</strong> của bạn đã được hệ thống ghi nhận. Nhân viên kỹ thuật sẽ sớm liên hệ xác nhận và chuẩn bị hàng cho bạn.
                </p>
            </div>

            <!-- Thông tin người nhận -->
            <div style='background:#f8fafc; border:1px solid #e2e8f0; border-radius:12px; padding:16px; margin-bottom:20px; font-size:14px;'>
                <div style='font-weight:700; color:#0f172a; margin-bottom:10px; font-size:15px;'>📍 Thông tin giao hàng</div>
                <div style='margin-bottom:6px;'><strong>Mã đơn hàng:</strong> <span style='color:#2563eb; font-weight:700;'>#{orderCode}</span></div>
                <div style='margin-bottom:6px;'><strong>Người nhận:</strong> {WebUtility.HtmlEncode(order.FullName)}</div>
                <div style='margin-bottom:6px;'><strong>Số điện thoại:</strong> {WebUtility.HtmlEncode(order.Phone)}</div>
                <div style='margin-bottom:6px;'><strong>Địa chỉ nhận:</strong> {WebUtility.HtmlEncode(order.Address)}</div>
                <div style='margin-bottom:6px;'><strong>Phương thức thanh toán:</strong> {paymentDisplay}</div>
                <div><strong>Thời gian đặt:</strong> {order.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}</div>
            </div>

            <!-- Bảng sản phẩm -->
            <div style='font-weight:700; color:#0f172a; margin-bottom:10px; font-size:15px;'>🛒 Chi tiết sản phẩm đã đặt</div>
            <table style='width:100%; border-collapse:collapse; font-size:14px; margin-bottom:20px;'>
                <thead>
                    <tr style='background:#f1f5f9; color:#334155; text-align:left;'>
                        <th style='padding:10px 12px; border-bottom:2px solid #cbd5e1;'>Sản phẩm</th>
                        <th style='padding:10px 12px; border-bottom:2px solid #cbd5e1;'>Phân loại</th>
                        <th style='padding:10px 12px; border-bottom:2px solid #cbd5e1; text-align:center;'>SL</th>
                        <th style='padding:10px 12px; border-bottom:2px solid #cbd5e1; text-align:right;'>Đơn giá</th>
                        <th style='padding:10px 12px; border-bottom:2px solid #cbd5e1; text-align:right;'>Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {rowsHtml}
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan='4' style='padding:10px 12px; text-align:right; color:#64748b;'>Phí vận chuyển:</td>
                        <td style='padding:10px 12px; text-align:right; font-weight:600;'>{shippingText}</td>
                    </tr>
                    <tr style='background:#fef2f2;'>
                        <td colspan='4' style='padding:12px; text-align:right; font-weight:800; color:#0f172a; font-size:15px;'>TỔNG THANH TOÁN:</td>
                        <td style='padding:12px; text-align:right; font-weight:800; color:#dc2626; font-size:16px;'>{order.Total:n0} đ</td>
                    </tr>
                </tfoot>
            </table>

            <!-- Hỗ trợ -->
            <div style='background:#eff6ff; border:1px solid #bfdbfe; border-radius:12px; padding:14px 16px; font-size:13px; color:#1e3a8a;'>
                📞 Cần hỗ trợ thay đổi thông tin hoặc tư vấn mức căng cước? Vui lòng gọi Hotline <strong>0900 000 000</strong> (8:00 - 21:30).
            </div>
        </div>

        <!-- Footer -->
        <div style='background:#f8fafc; border-top:1px solid #e2e8f0; padding:16px 24px; text-align:center; font-size:12px; color:#64748b;'>
            © {DateTime.Now.Year} <strong>Tiêu Đao Badminton Pro</strong> • 123 Đường Cầu Lông, TP. Hồ Chí Minh
        </div>
    </div>
</body>
</html>";
        }

        private static string BuildAdminEmailHtml(
            Order order,
            List<CartItem> items,
            string orderCode,
            string customerEmail,
            string paymentDisplay,
            decimal shippingFee)
        {
            var rowsHtml = BuildItemsTableRowsHtml(items);
            var shippingText = shippingFee == 0 ? "Miễn phí (0 đ)" : $"{shippingFee:n0} đ";

            return $@"
<!DOCTYPE html>
<html lang='vi'>
<body style='margin:0; padding:0; background-color:#f1f5f9; font-family: Arial, Helvetica, sans-serif; color:#1e293b;'>
    <div style='max-width:640px; margin:24px auto; background:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 8px 24px rgba(15,23,42,0.08); border:1px solid #e2e8f0;'>
        <!-- Header Admin -->
        <div style='background:linear-gradient(135deg,#dc2626 0%,#991b1b 100%); padding:24px; text-align:center; color:#ffffff;'>
            <div style='font-size:20px; font-weight:800;'>🔔 THÔNG BÁO ĐƠN HÀNG MỚI #{orderCode}</div>
            <div style='font-size:13px; color:#fee2e2; margin-top:4px;'>Hệ thống Quản trị Tiêu Đao Badminton vừa tiếp nhận đơn đặt hàng mới</div>
        </div>

        <!-- Body -->
        <div style='padding:24px;'>
            <div style='background:#fffbeb; border:1px solid #fde68a; border-radius:12px; padding:16px; margin-bottom:20px; font-size:14px;'>
                <div style='font-weight:800; color:#92400e; margin-bottom:10px; font-size:15px;'>👤 Thông tin khách đặt hàng</div>
                <div style='margin-bottom:6px;'><strong>Mã đơn hàng:</strong> <span style='color:#dc2626; font-weight:800;'>#{orderCode}</span> (ID: {order.Id})</div>
                <div style='margin-bottom:6px;'><strong>Họ và tên khách:</strong> {WebUtility.HtmlEncode(order.FullName)}</div>
                <div style='margin-bottom:6px;'><strong>Số điện thoại:</strong> <a href='tel:{WebUtility.HtmlEncode(order.Phone)}' style='color:#2563eb; font-weight:700;'>{WebUtility.HtmlEncode(order.Phone)}</a></div>
                <div style='margin-bottom:6px;'><strong>Email khách hàng:</strong> {WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(customerEmail) ? "Không cung cấp" : customerEmail)}</div>
                <div style='margin-bottom:6px;'><strong>Địa chỉ giao hàng & Ghi chú:</strong> {WebUtility.HtmlEncode(order.Address)}</div>
                <div style='margin-bottom:6px;'><strong>Hình thức thanh toán:</strong> {paymentDisplay}</div>
                <div><strong>Thời gian đặt:</strong> {order.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm:ss}</div>
            </div>

            <div style='font-weight:700; color:#0f172a; margin-bottom:10px; font-size:15px;'>📦 Danh sách sản phẩm cần chuẩn bị</div>
            <table style='width:100%; border-collapse:collapse; font-size:14px; margin-bottom:20px;'>
                <thead>
                    <tr style='background:#f1f5f9; color:#334155; text-align:left;'>
                        <th style='padding:10px 12px; border-bottom:2px solid #cbd5e1;'>Sản phẩm</th>
                        <th style='padding:10px 12px; border-bottom:2px solid #cbd5e1;'>Phân loại kho</th>
                        <th style='padding:10px 12px; border-bottom:2px solid #cbd5e1; text-align:center;'>SL</th>
                        <th style='padding:10px 12px; border-bottom:2px solid #cbd5e1; text-align:right;'>Đơn giá</th>
                        <th style='padding:10px 12px; border-bottom:2px solid #cbd5e1; text-align:right;'>Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {rowsHtml}
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan='4' style='padding:10px 12px; text-align:right; color:#64748b;'>Phí vận chuyển:</td>
                        <td style='padding:10px 12px; text-align:right; font-weight:600;'>{shippingText}</td>
                    </tr>
                    <tr style='background:#f0fdf4;'>
                        <td colspan='4' style='padding:12px; text-align:right; font-weight:800; color:#0f172a; font-size:15px;'>TỔNG GIÁ TRỊ ĐƠN HÀNG:</td>
                        <td style='padding:12px; text-align:right; font-weight:800; color:#15803d; font-size:16px;'>{order.Total:n0} đ</td>
                    </tr>
                </tfoot>
            </table>

            <div style='text-align:center; margin-top:20px;'>
                <p style='font-size:13px; color:#64748b; margin-bottom:12px;'>Vui lòng truy cập trang <strong>Quản lý Đơn hàng (/Admin/Orders)</strong> để xác nhận và tiến hành giao hàng.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}

