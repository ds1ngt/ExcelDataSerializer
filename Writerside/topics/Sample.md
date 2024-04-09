# 예제
## 일반 데이터 시트
### Excel (Data)

시트 이름 : User

| ID          | Age | Name   | Type              |
|-------------|-----|--------|-------------------|
| Primary/Int | Int | String | Enum/Get/UserType |
| 1           | 30  | John   | Admin             |
| 2           | 34  | Mark   | Guest             |

### 생성되는 클래스
```C#
using MemoryPack;

[MemoryPackable]
public partial class User
{
    public int ID { get; set; }
    public int Age { get; set; }
    public string Name { get; set; }
    public UserType Type { get; set; }
}

[MemoryPackable(GenerateType.Collection)]
public partial class UserTable : Dictionary<int, User>
{
    public static UserTable Get(string base64Str)
    {
        var bytes = System.Convert.FromBase64String(base64Str);
        return MemoryPackSerializer.Deserialize<UserTable>(bytes);
    }
} 
```

## Enum 데이터 시트
### Excel (Enum)
시트 이름 : Enum

| UserType   | MoveType    |
|------------|-------------|
| Enum / Set | Enum / Set  |
| Guest = 0  | None = 0    |
| Admin = 1  | Warp = 1    |
|            | Jump = 2    |
|            | Cross = 3   |
|            | Replace = 4 |

### 생성되는 클래스
```C#
public enum UserType
{
    Guest = 0,
    Admin = 1,
}

public enum MoveType
{
    None = 0,
    Warp = 1,
    Jump = 2,
    Cross = 3,
    Replace = 4,
}
```