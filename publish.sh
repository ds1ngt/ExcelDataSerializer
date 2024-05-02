dotnet publish\
    ./ExcelDataSerializerUI/ExcelDataSerializerUI.csproj\
     --runtime osx-arm64\
     --self-contained=true\
     /p:PublishSingleFile=true\
     -o Publish\