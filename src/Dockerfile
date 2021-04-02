#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["Bastille.Id.Api.csproj", "."]
RUN dotnet restore "./Bastille.Id.Api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Bastille.Id.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bastille.Id.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bastille.Id.Api.dll"]