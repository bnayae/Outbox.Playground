using Avro.File;
using Avro.Generic;
using Avro.Specific;

namespace Avro;

public static class AvroSerializationExtensions
{
    public static byte[] SerializeISpecificRecord<T>(this T item) where T : ISpecificRecord
    {
        DatumWriter<T> writer = new SpecificDatumWriter<T>(item.Schema);
        using var output = new MemoryStream(1024 * 5);

        using var specDataWriter = DataFileWriter<T>.OpenWriter(writer, output);
        specDataWriter.Append(item);
        specDataWriter.Flush();

        byte[] data = output.ToArray();
        return data;
    }

    public static T DeserializeISpecificRecord<T>(this byte[] data) where T : ISpecificRecord
    {
        // Deserialize using DataFileReader
        using (var input = new MemoryStream(data))
        {
            using var dataFileReader = DataFileReader<T>.OpenReader(input);

            //foreach (var deserialized in dataFileReader.NextEntries)
            //{
            //    Console.WriteLine($"Deserialized: Name={deserialized.Name}, Age={deserialized.Age}");
            //}

            return dataFileReader.NextEntries.First();
        }
    }
}

