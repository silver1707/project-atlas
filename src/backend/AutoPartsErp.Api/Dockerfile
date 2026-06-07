FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["src/backend/AutoPartsErp.Api/AutoPartsErp.Api.csproj", "src/backend/AutoPartsErp.Api/"]
RUN dotnet restore "src/backend/AutoPartsErp.Api/AutoPartsErp.Api.csproj"
COPY . .
RUN dotnet publish "src/backend/AutoPartsErp.Api/AutoPartsErp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN addgroup --system autoparts && adduser --system --ingroup autoparts autoparts
COPY --from=build /app/publish .
RUN mkdir -p /var/lib/autoparts-erp/fiscal-xml && chown -R autoparts:autoparts /var/lib/autoparts-erp
USER autoparts
EXPOSE 8080
ENTRYPOINT ["dotnet", "AutoPartsErp.Api.dll"]
