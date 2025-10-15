FluentValidation は「実行時検証」の仕組みなので、OpenAPI（Swagger）のスキーマ生成はタイプ情報や DataAnnotations を見るだけでは規則を自動で拾いません。反映したい場合は、Swagger/NSwag 用の“ブリッジ”を入れてルール→OpenAPI 制約へ変換させます。

どうすれば反映できる？
Swashbuckle（Swagger UI の定番）

パッケージ

FluentValidation.AspNetCore（ASP.NET Core での自動検証）

MicroElements.Swashbuckle.FluentValidation（← これがブリッジ）