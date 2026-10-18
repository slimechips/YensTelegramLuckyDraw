# Dockerfile for YensTelegramLuckyDraw
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . ./
RUN dotnet restore "YensTelegramLuckyDraw.csproj"
RUN dotnet publish "YensTelegramLuckyDraw.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Copy prize images from the source image folder into the published app's wwwroot so
# the controller can find them at /wwwroot/images/prizes/prize{Id}.jpg
COPY --from=build /src/img /app/wwwroot/images/prizes
ENTRYPOINT ["dotnet", "YensTelegramLuckyDraw.dll"]

