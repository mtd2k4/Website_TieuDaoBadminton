# QUY CHUẨN VÀ PHƯƠNG PHÁP VIẾT BÁO CÁO ĐỒ ÁN TỐT NGHIỆP (`BaoCaoTotNghiep.doc`)

Tài liệu này ghi chú toàn bộ các yêu cầu, quy tắc định dạng và phương pháp kỹ thuật đã thực hiện để xây dựng và chuẩn hóa tệp báo cáo [BaoCaoTotNghiep.doc](file:///c:/Users/maiti/Downloads/Do%20an%20mon%20hoc/tieudaobadminton/BaoCaoTotNghiep.doc). Bạn có thể dùng trực tiếp tài liệu này làm tập chỉ dẫn (Prompt / Rule) khi cần viết hoặc chỉnh sửa báo cáo đồ án.

---

## 1. Nguyên tắc tổng thể khi viết vào file `BaoCaoTotNghiep.doc`

1. **Viết trực tiếp vào file mẫu của trường nhưng giữ nguyên các trang đầu:**
   - Đọc cấu trúc đề mục từ file `MỤC LỤC BÁO CÁO ĐỒ ÁN TỐT NGHIỆP.docx` và điều chỉnh tên các đề mục cho khớp 100% với đề tài thực tế của dự án.
   - Khi ghi nội dung vào `BaoCaoTotNghiep.doc`, **bắt buộc giữ nguyên** không xóa/ghi đè các phần đầu gồm:
     - Trang bìa ngoài và Trang phụ bìa.
     - Trang **LỜI CẢM ƠN** (gửi lời cảm ơn sâu sắc tới giảng viên hướng dẫn – **thầy giáo Đỗ Cao Minh**).
     - Trang **NHẬN XÉT CỦA GIÁO VIÊN HƯỚNG DẪN**.
     - Khối **MỤC LỤC** tự động (`w:sdt` TOC) và ngắt phân vùng đánh số trang La Mã (`i, ii, iii...`) cho phần đầu / số trang Ả Rập (`1, 2, 3...`) từ phần `MỞ ĐẦU` trở đi.
2. **Tuyệt đối không dùng Emoji:**
   - Toàn bộ văn bản, bảng biểu, chú thích trong báo cáo phải dùng ngôn ngữ học thuật nghiêm túc, không chứa bất kỳ ký tự emoji nào.

---

## 2. Quy chuẩn tách trang Phần Mở Đầu (Front Matter)

Thứ tự các trang sau phần *Nhận xét của GVHD* và *Mục lục* phải được tách thành các trang riêng biệt (dùng style `PhnMu` kèm `<w:pageBreakBefore/>`):

1. **Trang `DANH MỤC HÌNH ẢNH` (Tách riêng 1 trang):**
   - Không gộp chung với bảng biểu.
   - Liệt kê đầy đủ từng hình theo thứ tự chương (`Hình 2.1`, `Hình 3.1` – `Hình 3.9`, `Hình 4.1` – `Hình 4.8`), có đường chấm dẫn (`tab leader="dot"`) sang số trang bên phải.
2. **Trang `DANH MỤC BẢNG BIỂU` (Sang trang mới riêng biệt):**
   - Tách riêng khỏi danh mục hình ảnh.
   - Liệt kê **từng bảng một** (ví dụ: `Bảng 1.1`, `Bảng 3.1`, `Bảng 3.2`, `Bảng 3.3`... đến `Bảng 3.23`, `Bảng 4.1`, `Bảng 5.1`, `Bảng 5.2`, `Bảng 5.3`, `Bảng C.1`), **không viết gộp cụm** kiểu `Bảng 3.2 – Bảng 3.9` hay `Bảng 3.10 – Bảng 3.23`.
3. **Trang `DANH MỤC TỪ VIẾT TẮT`:**
   - Trình bày dạng bảng gồm 3 cột: *Từ viết tắt*, *Cụm từ đầy đủ*, *Giải thích ý nghĩa trong hệ thống*.
4. **Trang `TÓM TẮT ĐỒ ÁN`:**
   - Tóm tắt ngắn gọn trong 3 đoạn văn về mục tiêu đề tài, các tính năng phía Khách hàng (Storefront) và các tính năng phía Quản trị (Admin).

---

## 3. Quy tắc trình bày Bảng biểu (Tables) và Chú thích dưới chân bảng

1. **Tuyệt đối không để tên bảng ở phía trên bảng:**
   - **Không viết** các câu dẫn có số hiệu bảng ở phía trên như: *"Bảng 3.1 mô tả chi tiết 4 tác nhân..."* hoặc *"Bảng 1.1 dưới đây tổng hợp..."*.
   - **Không đặt** số hiệu bảng vào tiêu đề mục nhỏ phía trên bảng như: *"1) Bảng 5.1: Kết quả kiểm thử..."* hay *"1) Bảng Categories..."*.
2. **Cách viết phần phía trên bảng:**
   - Chỉ dùng câu dẫn tự nhiên (ví dụ: *"Hệ thống bao gồm 4 tác nhân tương tác trực tiếp với phạm vi quyền hạn được phân định cụ thể như sau:"*) hoặc tiêu đề mục nhỏ thuần túy (ví dụ: *"1) Thực thể Categories (Danh mục sản phẩm)"*, *"1) Kiểm thử chức năng Phân hệ Khách hàng (Storefront)"*).
3. **Đặt tên bảng ở ngay dưới chân bảng (Bottom Table Caption):**
   - Ngay sau thẻ đóng `</w:tbl>` của mỗi bảng, bắt buộc có dòng chú thích tên bảng đặt ở **dưới chân bảng**.
   - Định dạng dòng chú thích dưới chân bảng:
     - Căn giữa trang (`Center`), không thụt đầu dòng (`firstLine="0"`), cỡ chữ `12pt` (`Times New Roman`).
     - Phần tiền tố **`Bảng X.Y:`** in đậm + in nghiêng; phần nội dung tên bảng phía sau in nghiêng (ví dụ: ***Bảng 3.1:*** *Mô tả chi tiết 4 tác nhân (Actors) tương tác trực tiếp với hệ thống Tiêu Đao Badminton*).
4. **Định dạng bên trong bảng:**
   - Chiều rộng bảng 100% khổ trang (`w:w="5000" w:type="pct"`), viền mảnh màu xám xanh (`#64748B` / `#CBD5E1`).
   - Dòng tiêu đề cột (`Header Row`): Nền xám nhạt (`#E2E8F0`), chữ căn giữa, **in đậm**.
   - Các dòng dữ liệu (`Data Rows`): Căn trái, **chữ thường hoàn toàn** (không in đậm cột đầu tiên).

---

## 4. Quy tắc về In đậm (Bolding) và Danh sách liệt kê (`ListParagraph`)

1. **Chỉ in đậm tại các đề mục (Headings):**
   - Chỉ được phép in đậm ở:
     - Tiêu đề Phần mở đầu (`PhnMu`) và Tiêu đề Chương (`Chng`).
     - Tiêu đề mục cấp 2 (`1.1`, `1.2`... - style `11`).
     - Tiêu đề mục cấp 3 (`1.1.1`, `1.1.2`... - style `111`).
     - Tiêu đề tiểu mục nhỏ (`1) ...`, `2) ...`, `a) ...`, `b) ...`).
     - Hàng tiêu đề cột của bảng biểu.
2. **Tuyệt đối không in đậm trong các danh sách liệt kê (`ListParagraph`):**
   - Mọi dòng gạch đầu dòng trong báo cáo phải để **chữ thường toàn bộ** (`Times New Roman`, `13pt`, không có thẻ `<w:b/>`).
   - Ví dụ: Viết chữ thường nguyên dòng `- Mô hình B2B (Business to Business): Giao dịch thương mại giữa các doanh nghiệp...`, **không** bôi đậm cụm *"Mô hình B2B (Business to Business): "*.
   - Lưu ý kỹ thuật trong Word XML:
     - Style `ListParagraph` của file mẫu đã gắn sẵn `numId="7"` (`abstractNumId="19"`) tự động sinh dấu gạch đầu dòng `-`. Vì vậy nội dung chữ đưa vào không được gõ thêm dấu `- ` ở đầu (tránh bị lặp `- -`), đồng thời phải xóa thẻ `<w:b/>` trong `word/numbering.xml` tại `abstractNumId="19"` để dấu `-` đầu dòng cũng không bị in đậm.

---

## 5. Quy tắc tạo Khung chờ Hình ảnh (Figure Placeholders) và Vị trí đặt Sơ đồ

1. **Định dạng khung chờ hình ảnh:**
   - Tạo đoạn văn có khung viền nét đứt (`w:val="dashed"`, màu `#94A3B8`), nền `#F8FAFC`, căn giữa, ghi rõ `[Khung chờ: Chèn ảnh ... tại đây]`.
   - Ngay **dưới chân khung chờ** là dòng chú thích hình căn giữa, in nghiêng theo mẫu: ***Hình X.Y:*** *Tên mô tả hình ảnh*.
2. **Vị trí đặt các biểu đồ trong Chương 2, Chương 3 và Chương 4:**
   - **Chương 2 – Mục `2.1.2`:** Đặt `Hình 2.1: Sơ đồ kiến trúc ASP.NET Core 8.0 MVC của hệ thống Tiêu Đao Badminton`.
   - **Chương 3 – Mục `3.2.1` (Biểu đồ Use Case tổng quát và chi tiết):**
     - Đặt tập trung 3 hình theo thứ tự từ tổng quát đến phân rã:
       - `Hình 3.1: Biểu đồ Use Case tổng quát của hệ thống Tiêu Đao Badminton` (ngay dưới đoạn dẫn mục `3.2.1`).
       - `Hình 3.2: Biểu đồ Use Case chi tiết Phân hệ Khách hàng (Storefront)` (ngay dưới phần mô tả nhóm Use Case Khách hàng).
       - `Hình 3.3: Biểu đồ Use Case chi tiết Phân hệ Quản trị (Enterprise Admin Pro)` (ngay dưới phần mô tả nhóm Use Case Quản trị).
     - Sau đó sang **Mục `3.2.2`** chỉ trình bày 8 bảng đặc tả Use Case (`Bảng 3.2` đến `Bảng 3.9`).
   - **Chương 3 – Mục `3.2.3` (Biểu đồ hoạt động - Activity Diagram):** Đặt `Hình 3.4` (Quy trình Đặt hàng, áp mã giảm giá Voucher & VietQR) và `Hình 3.5` (Quy trình Đăng ký khóa học gửi SMTP về Admin).
   - **Chương 3 – Mục `3.2.4` (Biểu đồ tuần tự - Sequence Diagram):** Đặt `Hình 3.6`.
   - **Chương 3 – Mục `3.2.5` (Biểu đồ lớp - Class Diagram):** Đặt `Hình 3.7`.
   - **Chương 3 – Mục `3.3.1` (Mô hình thực thể liên kết - ERD):** Đặt `Hình 3.8`.
   - **Chương 3 – Mục `3.5.1` (Sơ đồ điều hướng tổng thể - Sitemap):** Đặt `Hình 3.9`.
   - **Chương 4 – Các mục `4.2` và `4.3`:** Đặt các khung chờ chụp màn hình giao diện thực tế từ `Hình 4.1` đến `Hình 4.8`.

---

## 6. Quy tắc ánh xạ Style của Word để tự động đánh số đề mục và cập nhật Mục lục

Khi sinh XML cho `word/document.xml`, phải gắn đúng `w:pStyle` của file gốc:
- **`PhnMu`:** Dùng cho các tiêu đề lớn không đánh số chương (*Mục lục, Danh mục hình ảnh, Danh mục bảng biểu, Danh mục từ viết tắt, Tóm tắt đồ án, Mở đầu, Kết luận và hướng phát triển, Tài liệu tham khảo, Phụ lục*).
- **`Chng`:** Dùng cho tiêu đề 5 chương. Style này trong file gốc đã tự động sinh chữ `CHƯƠNG 1.`, `CHƯƠNG 2.`... nên trong thẻ `<w:t>` chỉ truyền tên chương (ví dụ: `"TỔNG QUAN VỀ ĐỀ TÀI"`), không gõ lặp chữ `"CHƯƠNG 1"`.
- **`11` (Heading cấp 2):** Style này đã tự động đánh số `1.1`, `1.2`, `2.1`... nên trong thẻ `<w:t>` chỉ truyền tên mục (ví dụ: `"Tổng quan về thương mại điện tử"`), không gõ lặp số `"1.1"`.
- **`111` (Heading cấp 3):** Style này đã tự động đánh số `1.1.1`, `1.1.2`... nên trong thẻ `<w:t>` chỉ truyền tên mục (ví dụ: `"Khái niệm và đặc điểm của thương mại điện tử"`), không gõ lặp số `"1.1.1"`.
