#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SubscriptionManagementApi/SubscriptionManagementApi.csproj", "SubscriptionManagementApi/"]
RUN dotnet restore "SubscriptionManagementApi/SubscriptionManagementApi.csproj"
COPY . .
WORKDIR "/src/SubscriptionManagementApi"
RUN dotnet build "SubscriptionManagementApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SubscriptionManagementApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SubscriptionManagementApi.dll"]