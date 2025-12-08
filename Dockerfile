# --- AŞAMA 1: İNŞAAT (BUILD) ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# DÜZELTME: Artık klasör ismi belirtmiyoruz, çünkü zaten o klasördeyiz.
COPY ["WorkFlowBasic.csproj", "."]
RUN dotnet restore "./WorkFlowBasic.csproj"

# Tüm dosyaları kopyala
COPY . .

# Build işlemi
WORKDIR "/src/."
RUN dotnet build "WorkFlowBasic.csproj" -c Release -o /app/build

# --- AŞAMA 2: YAYINLAMA (PUBLISH) ---
FROM build AS publish
RUN dotnet publish "WorkFlowBasic.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- AŞAMA 3: ÇALIŞTIRMA (RUNTIME) ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorkFlowBasic.dll"]