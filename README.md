# ExcelDataSerializer
Excel데이터를 런타임 환경에서 사용할 수 있는 형태로 가공하는 유틸리티
## 프로젝트 진입점
### ExcelDataSerializer (Core)
- [Runner.cs : ExecuteAsync](https://github.com/ds1ngt/ExcelDataSerializer/blob/develop/ExcelDataSerializer/Runner.cs)
### ExcelDataSerializerUI (GUI)
- [MainWindow.axaml.cs : OnExecute](https://github.com/ds1ngt/ExcelDataSerializer/blob/develop/ExcelDataSerializerUI/Views/MainWindow.axaml.cs)
## 사용 기술
- Excel 데이터 파싱: XlsxHelper
- 코드 생성: CodeDOM
- 데이터 직렬화: MessagePack
- GUI: Avalonia
## 배포
### Mac 버전 배포
- `publish.sh` 스크립트 실행하면 Publish 폴더 내에 배포용 빌드 생성