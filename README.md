# 📝 FitRehber - Diyetisyen Klinik Yönetim Sistemi

**FitRehber**, diyetisyen kliniklerinin dijital dönüşümünü sağlamak amacıyla geliştirilmiş, uçtan uca çözümler sunan web tabanlı bir otomasyon sistemidir. Hasta takibi, randevu yönetimi ve diyet planlama gibi süreçleri tek bir platformda toplar.

---

## 🏗️ Mimari Yapı (N-Tier Architecture)

Proje, **Clean Architecture** prensiplerine uygun olarak 4 ana katmandan oluşmaktadır:

* **Entity Layer:** Veritabanı modellerinin (User, Appointment, DietPlan vb.) bulunduğu katman.
* **DataAccess Layer:** Entity Framework Core kullanarak veritabanı işlemlerinin (CRUD) yürütüldüğü katman.
* **Business Layer:** Sistemin mantıksal süreçlerinin, doğrulama kurallarının ve servislerin yönetildiği katman.
* **API Layer:** Dış dünyaya açılan, isteklere cevap veren ve JWT tabanlı güvenliğin sağlandığı katman.

---

## 🛠️ Teknoloji Stack'i

| Katman | Teknoloji | Açıklama |
| :--- | :--- | :--- |
| **Backend** | .NET 8 ASP.NET Core | Modern ve cross-platform web framework. |
| **Veritabanı** | SQL Server (LocalDB) | Geliştirme ortamı için ilişkisel veritabanı. |
| **ORM** | Entity Framework Core | Database-first / Code-first veritabanı yönetimi. |
| **Auth** | JWT (JSON Web Token) | Stateless ve güvenli kimlik doğrulama. |
| **Real-time** | SignalR (WebSocket) | Anlık mesajlaşma ve bildirim altyapısı. |
| **Frontend** | Vanilla JS / CSS / HTML | Framework bağımsız saf web teknolojileri. |
| **Dokümantasyon** | Swagger / OpenAPI | Otomatik API test ve dokümantasyon arayüzü. |

---

## ✨ Temel Özellikler & Teknik Detaylar

### 🔐 Güvenlik ve Yetkilendirme
* **Role Based Authorization:** Admin, Diyetisyen ve Hasta rolleriyle özelleşmiş erişim kontrolü.
* **BCrypt Hashing:** Şifreler veritabanında yüksek güvenlikli hash algoritmaları ile saklanır.
* **Rate Limiting:** Brute-force saldırılarını önlemek için istek sınırlama mekanizması.
* **Account Lockout:** Hatalı giriş denemelerinde otomatik hesap kilitleme.

### 💬 Real-time İletişim (SignalR)
Diyetisyen ve hastalar arasında anlık mesajlaşma imkanı sağlar. WebSocket protokolü sayesinde iletişim kesintisiz ve canlıdır.

### 📅 Arka Plan Görevleri (Background Service)
`IHostedService` kullanılarak oluşturulan **AppointmentReminderService**, yaklaşan randevuları periyodik olarak kontrol eder ve e-posta/sistem bildirimi gönderir.

---

## 🚀 Kurulum

1.  Projeyi yerel bilgisayarınıza klonlayın:
    ```bash
    git clone [https://github.com/agzibuyukasli/FitRehber.git](https://github.com/agzibuyukasli/FitRehber.git)
    ```
2.  `appsettings.json` dosyasındaki veritabanı bağlantı dizgesini (Connection String) güncelleyin.
3.  Migration'ları uygulayın:
    ```bash
    dotnet ef database update
    ```
4.  Projeyi ayağa kaldırın:
    ```bash
    dotnet run
    ```

---

## 👨‍💻 Geliştirici
**Aslı Ağzıbüyük** - [GitHub Profilim](https://github.com/agzibuyukasli)

---
*Bu proje bir staj geliştirme süreci kapsamında hazırlanmıştır.*
