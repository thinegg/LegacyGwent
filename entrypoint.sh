#!/bin/bash
# the entrypoint for web coantiner in docker-compose.
dotnet build /app/src/Cynthia.Card/src/Cynthia.Card.Server/Cynthia.Card.Server.csproj
cp /app/src/Cynthia.Card/src/Cynthia.Card.Common/bin/Debug/netstandard2.0/Cynthia.Card.Common.dll /app/src/Cynthia.Card.Unity/src/Cynthia.Unity.Card/Assets/Assemblies/Cynthia.Card.Common.dll
dotnet watch --project /app/src/Cynthia.Card/src/Cynthia.Card.Server/Cynthia.Card.Server.csproj run
