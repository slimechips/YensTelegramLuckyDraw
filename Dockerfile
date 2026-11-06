# Dockerfile for YensTelegramLuckyDraw
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# Ensure ASP.NET listens on the HTTP ports Azure expects. Bind to port 80 (and 8080 as fallback).
ENV ASPNETCORE_URLS="http://+:80;http://+:8080"
EXPOSE 80 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . ./
RUN dotnet restore "YensTelegramLuckyDraw.csproj"
RUN dotnet publish "YensTelegramLuckyDraw.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/Puzzles /app/Puzzles
# Copy prize images from the source image folder into the published app's wwwroot so
# the controller can find them at /wwwroot/images/prizes/prize{Id}.jpg
COPY --from=build /src/img /app/wwwroot/images/prizes
ENTRYPOINT ["dotnet", "YensTelegramLuckyDraw.dll"]
