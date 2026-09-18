# WorkFlow — İş Akışı ve Onay Yönetim Sistemi

[English](README.md) | **Türkçe**

ASP.NET Core MVC ile geliştirilmiş, şirket içi talep/onay süreçlerini, görevleri,
envanteri ve personel yönetimini tek bir yerde toplayan web uygulaması.

## Özellikler

- **Rol tabanlı yetkilendirme** — Admin, Yönetici (Manager) ve Personel rolleri
  (ASP.NET Core Identity, Türkçe hata mesajları)
- **Talep ve onay akışı** — talep oluşturma, yöneticiye onaya gönderme, onaylama /
  reddetme / revizyon isteme, toplu onay, işlem geçmişi (log)
- **Görev yönetimi** — görev atama ve takip, takvim görünümü
- **Envanter yönetimi** — demirbaş kaydı, personele zimmetleme, zimmet geçmişi,
  Excel'den toplu içe aktarma
- **Personel kaydı** — CV yüklemeli kayıt, yönetici onayıyla hesap aktivasyonu
- **Gerçek zamanlı bildirimler** — SignalR
- **Sohbet botu** — sistem verileri üzerinden soruları yanıtlayan kural tabanlı asistan
- **E-posta bildirimleri** — SMTP
- **Arka plan temizlik servisi** — eski kayıtların otomatik temizlenmesi
- **REST API** — Swagger arayüzü ile belgelenmiş iş akışı uç noktaları

## Teknolojiler

ASP.NET Core (.NET 10) MVC · Entity Framework Core · SQL Server · ASP.NET Core
Identity · SignalR · Swagger · Bootstrap · Docker

## Çalıştırma

### Docker ile

```bash
cp .env.example .env      # SA_PASSWORD değerini düzenleyin
docker compose up --build
```

Uygulama `http://localhost:5000` adresinde açılır.

### Yerelde

`appsettings.json` içindeki bağlantı dizesi varsayılan olarak SQL Server LocalDB'yi
kullanır.

```bash
dotnet ef database update
dotnet run
```

E-posta gönderimi için `appsettings.json` içindeki `EmailSettings` bölümüne kendi
SMTP bilgilerinizi girin (Gmail için uygulama şifresi kullanılmalıdır).

## Proje Yapısı

```
Controllers/   MVC ve API controller'ları
Data/          DbContext ve başlangıç verisi (roller, varsayılan kullanıcılar)
Hubs/          SignalR hub'ı
Models/        Varlıklar ve ViewModel'ler
Services/      E-posta, bildirim, sohbet botu, arka plan servisleri
Views/         Razor görünümleri
Migrations/    EF Core migration'ları
```
