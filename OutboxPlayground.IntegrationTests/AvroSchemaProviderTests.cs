// Detailed plan (pseudocode):
// 1. Define a simple POCO `Person` with a few properties (Name, Age, CreatedAt).
// 2. Reflect over the Person type's public instance properties to produce Avro field definitions.
//    - Map C# types to Avro JSON types:
//        string -> "string" (provide default empty string to make schema stable)
//        int    -> "int"
//        long   -> "long"
//        DateTime -> {"type":"long","logicalType":"timestamp-millis"}
//        Other types -> "string" (fallback for this small example)
//    - Build each field JSON fragment with the Avro required keys: "name" and "type" (and "default" when appropriate).
// 3. Assemble a minimal Avro record schema JSON string with "type":"record", "name", "namespace", and the fields array.
// 4. Use the Apache.Avro library's `Schema.Parse(string)` to validate/parse the generated JSON string into an Avro Schema object.
// 5. Assert basic properties on the produced schema JSON string and ensure it mentions expected names/fields.
// 6. Keep the test self-contained and avoid third-party Avro helpers so the intent is explicit and easy to adapt.
//
// Note: This test uses Apache.Avro (Schema.Parse) to validate the produced schema JSON string.
//       The mapping is intentionally minimal and tailored to the `Person` POCO used here.

using Xunit;

namespace OutboxPlayground.IntegrationTests;
public class Person
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public DateTime CreatedAt { get; set; }

    public Address? HomeAddress { get; set; }
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class AvroSchemaProviderTests
{

    [Fact]
    public void GetAvroSchema_FromDotNetClass_ReturnsSchemaContainingExpectedFields()
    {
        // Schema
    }
}
