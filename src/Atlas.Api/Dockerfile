FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["src/Atlas.Api/Atlas.Api.csproj", "src/Atlas.Api/"]
RUN dotnet restore "src/Atlas.Api/Atlas.Api.csproj"
COPY . .
RUN dotnet publish "src/Atlas.Api/Atlas.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN addgroup --system atlas && adduser --system --ingroup atlas atlas
COPY --from=build /app/publish .
RUN mkdir -p /var/lib/atlas/fiscal-xml && chown -R atlas:atlas /var/lib/atlas
USER atlas
EXPOSE 8080
ENTRYPOINT ["dotnet", "Atlas.Api.dll"]
