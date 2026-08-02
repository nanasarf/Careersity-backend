FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Careersity.sln Directory.Build.props ./
COPY src/Careersity.Domain/Careersity.Domain.csproj src/Careersity.Domain/
COPY src/Careersity.Application/Careersity.Application.csproj src/Careersity.Application/
COPY src/Careersity.Infrastructure/Careersity.Infrastructure.csproj src/Careersity.Infrastructure/
COPY src/Careersity.Api/Careersity.Api.csproj src/Careersity.Api/
RUN dotnet restore src/Careersity.Api/Careersity.Api.csproj
COPY src/ src/
RUN dotnet publish src/Careersity.Api/Careersity.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
COPY --from=build --chown=app:app /app/publish .
ENTRYPOINT ["dotnet", "Careersity.Api.dll"]
