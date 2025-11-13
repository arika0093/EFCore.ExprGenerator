# EFCore.ExprGenerator

EntityFrameworkCore(EFCore)におけるSelectクエリの記述を簡潔にし、DTOクラスの自動生成・nullable式のサポートを提供します。

[English](./README.md) | [Japanese](./README.ja.md)

## 課題
EFCoreにおいて、関連テーブルが大量にあるテーブルのデータを取得する例を考えます。

`Include`や`ThenInclude`を使用する方法は、すぐにコードが複雑になり可読性が低下します。  
また、Includeを忘れると実行時に`NullReferenceException`が発生する上、それを検知することは難しい難点があります。  
さらに、全てのデータを取得する関係上、パフォーマンス上でも問題があります。

```csharp
var orders = await dbContext.Orders
    .Include(o => o.Customer)
        .ThenInclude(c => c.Address)
            .ThenInclude(a => a.Country)
    .Include(o => o.Customer)
        .ThenInclude(c => c.Address)
            .ThenInclude(a => a.City)
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
    .ToListAsync();
```

より理想的な方法はDTO(Data Transfer Object)を使用し、必要なデータのみを選択的に取得することです。

```csharp
var orders = await dbContext.Orders
    .Select(o => new OrderDto
    {
        Id = o.Id,
        CustomerName = o.Customer.Name,
        CustomerCountry = o.Customer.Address.Country.Name,
        CustomerCity = o.Customer.Address.City.Name,
        Items = o.OrderItems.Select(oi => new OrderItemDto
        {
            ProductName = oi.Product.Name,
            Quantity = oi.Quantity
        }).ToList()
    })
    .ToListAsync();
```

上記の方法は、必要なデータのみを取得できるためパフォーマンス上大きな利点があります。
しかし、以下のような欠点があります。

* 匿名型を利用することは可能ですが、他の関数に渡す場合や返却値として使用する場合、手動でDTOクラスを定義する必要があります。
* nullableなプロパティを持つ子オブジェクトが存在する場合、三項演算子を駆使した冗長なコードを書く必要があります。

Expression内ではnullable演算子が利用できない性質上、`o.Customer?.Name`のような記述ができず、以下のようなコードになりがちです。

```csharp
var orders = await dbContext.Orders
    .Select(o => new OrderDto
    {
        Id = o.Id,
        CustomerName = o.Customer != null ? o.Customer.Name : null,
        CustomerCountry = o.Customer != null && o.Customer.Address != null && o.Customer.Address.Country != null
            ? o.Customer.Address.Country.Name
            : null,
        CustomerCity = o.Customer != null && o.Customer.Address != null && o.Customer.Address.City != null
            ? o.Customer.Address.City.Name
            : null,
        Items = o.OrderItems != null
            ? o.OrderItems.Select(oi => new OrderItemDto
            {
                ProductName = oi.Product != null ? oi.Product.Name : null,
                Quantity = oi.Quantity
            }).ToList()
            : new List<OrderItemDto>()
    })
    .ToListAsync();
```

## 特徴
EFCore.ExprGeneratorは、上記の問題を解決するために設計されたSource Generatorです。
上記の例では、以下のように記述することができます。

```csharp
var orders = await dbContext.Orders
    // Order: input entity type
    // OrderDto: output DTO type (auto-generated)
    .SelectExpr<Order, OrderDto>(o => new
    {
        Id = o.Id,
        CustomerName = o.Customer?.Name,
        CustomerCountry = o.Customer?.Address?.Country?.Name,
        CustomerCity = o.Customer?.Address?.City?.Name,
        Items = o.OrderItems?.Select(oi => new
        {
            ProductName = oi.Product?.Name,
            Quantity = oi.Quantity
        })
    })
    .ToListAsync();
```

`SelectExpr`のジェネリクス引数に`OrderDto`を指定することで、関連するDTOクラスを自動生成します。
匿名型セレクターから自動的にコードを生成するため、`OrderDto`や`OrderItemDto`を手動で定義する必要はありません。
例えば、上記の例では以下のようなメソッドおよびクラスが生成されます。

<details>
<summary>生成されたコード例</summary>

```csharp
// TODO
```

</details>

## 使用方法
### インストール
`EFCore.ExprGenerator`をNuGetからインストールします。

```
dotnet add package EFCore.ExprGenerator --prerelease
```

そして、csprojでインターセプターを有効にします。

```xml
<Project>
  <PropertyGroup>
    <!-- add EFCore.ExprGenerator to the InterceptorsPreviewNamespaces -->
    <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);EFCore.ExprGenerator</InterceptorsPreviewNamespaces>
  </PropertyGroup>
</Project>
```

### 利用例
以下のように`SelectExpr`メソッドを使用します。

```csharp
var orders = await dbContext.Orders
    // Order: input entity type
    // OrderDto: output DTO type (auto-generated)
    .SelectExpr<Order, OrderDto>(o => new
    {
        Id = o.Id,
        CustomerName = o.Customer?.Name,
        // ...
    })
    .ToListAsync();
```

自動生成機能を使用せず、既存のDtoクラスを利用することも可能です。この場合、ジェネリクス引数を指定しなくても構いません。

```csharp
var orders = await dbContext.Orders
    .SelectExpr(o => new OrderDto
    {
        Id = o.Id,
        CustomerName = o.Customer?.Name,
        // ...
    })
    .ToListAsync();
```

ジェネリクスを指定せずに匿名型を渡した場合、そのまま匿名型が返されます。

```csharp
var orders = await dbContext.Orders
    .SelectExpr(o => new
    {
        Id = o.Id,
        CustomerName = o.Customer?.Name,
        // ...
    })
    .ToListAsync();
var firstOrder = orders.First();
Console.WriteLine(firstOrder.GetType().Name); // -> anonymous type
```

## ライセンス
このプロジェクトはApache License 2.0の下でライセンスされています。
