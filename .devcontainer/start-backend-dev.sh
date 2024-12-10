echo "Starting backend"
(cd ../app/backend && dotnet restore)
(cd ../app/backend && dotnet run --urls=http://0.0.0.0:8000/)