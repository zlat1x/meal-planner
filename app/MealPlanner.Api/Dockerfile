FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["MealPlanner.Api/MealPlanner.Api.csproj", "MealPlanner.Api/"]
COPY ["MealPlanner.Domain/MealPlanner.Domain.csproj", "MealPlanner.Domain/"]
COPY ["MealPlanner.Infrastructure/MealPlanner.Infrastructure.csproj", "MealPlanner.Infrastructure/"]
RUN dotnet restore "MealPlanner.Api/MealPlanner.Api.csproj"
COPY . .
WORKDIR "/src/MealPlanner.Api"
RUN dotnet publish "MealPlanner.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MealPlanner.Api.dll"]
