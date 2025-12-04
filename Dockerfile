# --- AŞAMA 1: İNŞAAT (BUILD) ---
# Microsoft'un .NET 8 SDK'sını (Geliştirme Kiti) indiriyoruz.
# Bu imajın içinde kodları derlemek için gereken her şey var.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Konteynırın içinde '/src' diye bir klasör aç ve oraya gir.
WORKDIR /src

# Senin bilgisayarındaki proje dosyasını (.csproj) içeri kopyala.
COPY ["WorkFlowBasic.csproj", "."]

# İnternetten gerekli kütüphaneleri (NuGet paketlerini) indir.
RUN dotnet restore "./WorkFlowBasic.csproj"

# Şimdi kalan tüm kod dosyalarını (Controller, Views vb.) içeri kopyala.
COPY . .

# Kodları derle (Build et).
WORKDIR "/src/."
RUN dotnet build "WorkFlowBasic.csproj" -c Release -o /app/build

# --- AŞAMA 2: YAYINLAMA (PUBLISH) ---
# Derlenen kodları "Canlıya Çıkacak" şekilde optimize et ve paketle.
FROM build AS publish
RUN dotnet publish "WorkFlowBasic.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- AŞAMA 3: ÇALIŞTIRMA (RUNTIME) ---
# Burası Final kısmı. Artık SDK'ya (Ağır aletlere) ihtiyacımız yok.
# Sadece uygulamayı çalıştıracak hafif "ASP.NET Runtime" imajını alıyoruz.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Çalışma klasörünü ayarla
WORKDIR /app

# Yayınlama aşamasında (Aşama 2) oluşturduğumuz paketleri buraya al.
COPY --from=publish /app/publish .

# Ve son olarak: Uygulamayı başlat!
ENTRYPOINT ["dotnet", "WorkFlowBasic.dll"]