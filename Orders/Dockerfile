# Use the official .NET SDK image to build the application
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file
COPY *.sln ./

# Copy each project file to the appropriate folder structure
COPY OrdersSystem.Domain/OrdersSystem.Domain.csproj OrdersSystem.Domain/
COPY OrdersSystem.Infra/OrdersSystem.Infra.csproj OrdersSystem.Infra/
COPY OrdersSystem.Presentation.Api/OrdersSystem.Presentation.Api.csproj OrdersSystem.Presentation.Api/
COPY OrdersSystem.Http/OrdersSystem.Http.csproj OrdersSystem.Http/
COPY OrdersSystem.Application/OrdersSystem.Application.csproj OrdersSystem.Application/

# Restore dependencies for the entire solution
RUN dotnet restore

# Copy all source code files
COPY . .

# Build and publish the OrdersSystem.Http project
WORKDIR /src/OrdersSystem.Http
RUN dotnet publish -c Release -o /app

# Stage 2: Build a runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Expose the port your API will run on
EXPOSE 80

# Set the entrypoint to run the API project
ENTRYPOINT ["dotnet", "OrdersSystem.Http.dll"]
