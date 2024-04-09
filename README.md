# ExcelDataSerializer
Excel데이터를 런타임 환경에서 사용할 수 있는 형태로 가공하는 유틸리티
## 사용 방법
### ExcelDataSerializerConsole
빌드 후 생성된 프로그램에 다음 인자를 추가하여 실행
순서 | 인자
--- | --- 
1 | Excel 폴더 경로
2 | C# 클래스를 저장 할 경로
3 | 데이터를 저장 할 경로

``` shell
# 예시
ExcelDataSerializerConsole.exe C:\ExcelData C:\Output\CSharp C:\Output\Data
```

## 비고
- 현재는 임시로 Excel -> Json 변환만 제공, Excel -> MemoryPack 변환하는 구조로 변경 예정