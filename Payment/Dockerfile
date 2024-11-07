FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *sln ./

COPY PaymentSystem.Application/PaymentSystem.Application.csproj PaymentSystem.Application/
COPY PaymentSystem.Domain/PaymentSystem.Domain.csproj PaymentSystem.Domain/
COPY PaymentSystem.Infra/PaymentSystem.Infra.csproj PaymentSystem.Infra/
COPY PaymentSystem.Presentation.Api/PaymentSystem.Presentation.Api.csproj PaymentSystem.Presentation.Api/
COPY PaymentSystem.Http/PaymentSystem.Http.csproj PaymentSystem.Http/

RUN dotnet restore

COPY . .

WORKDIR /src/PaymentSystem.Http
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 81

ENTRYPOINT ["dotnet", "PaymentSystem.Http.dll"]