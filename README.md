# Specflow.Extensions
Extension for Specflow

It allows you to specify a deeper object graph within your gherkin table using normal dot syntax for example:
```csharp
Scenario: Create deep model instance
	When the following deep properties are provided:
	| Name                       | Dec            | Coll[0].Name  | Coll[0].Dec |  Coll[0].Name  | Coll[0].Dec   | 
	| Name                       | My Index 1     | My Coll 0 Name| 1.1         |  My Coll 0 Name| 1             |
 ```
By calling:

```csharp
[When(@"the following deep properties are hydrated:")]
public void WhenTheFollowingDeepPropertiesAreHydrated(Table table)
{
    var deepTable = table.CreateDeepInstance<DeepInstanceModel>();
}
```
A populated instance of the following class is created:

```csharp
public class DeepInstanceModel
{
    public string Name { get; set; }
 
    public decimal Dec { get; set; }
 
    public List<DeepInstanceModel> Coll { get; set; }
}
```
or
```csharp
Scenario: Create deep instance Of collection 
	When the following deep properties are provided:
	| collection[0].Property                   | collection[0].Value          |
	| Name                                     | My Index 1                   |
```
  By calling:
```csharp
[When(@"the following deep properties are hydrated:")]
public void WhenTheFollowingDeepPropertiesAreHydrated(Table table)
{
    var deepTable = table.CreateDeepInstance<DeepInstanceModel>();
}
``` 
A populated instance of the following collection IEnumerable<DeepInstanceModel>

#ToDo Add test
