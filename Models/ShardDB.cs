using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Connect.DB
{
    /// <summary>
    /// A document store database that splits its objects by primary key into several different files in a directory. By default, ShardDB creates a directory, then saves each file using a .shddb extension.
    /// </summary>
    public class ShardDB<T> : IDB<T>
    {
        private string extension = ".shddb";
        private string encryptionkey;
        private bool encrypted = false;

        public string DirectoryPath { get; private set; }

        public ShardDB(string directory, string password = null)
        {
            if (!string.IsNullOrWhiteSpace(password))
            {
                encrypted = true;
                encryptionkey = password;
            }

            DirectoryPath = directory;
            Directory.CreateDirectory(DirectoryPath);

        }

        //Finds all files in the directory path, and builds a dictionary out of them.
        public IDictionary<int, T> Read()
        {
            var files = Directory.GetFiles(DirectoryPath, "*" + extension, SearchOption.TopDirectoryOnly);

            var rows = new System.Collections.Concurrent.ConcurrentDictionary<int, T>();

            if (encrypted)
            {
                using (var service = new Cryptography(encryptionkey))
                {
                    Parallel.ForEach(files, file =>
                    {
                        try
                        {
                            int id = int.Parse(Path.GetFileNameWithoutExtension(file));
                            var jsonstring = File.ReadAllText(file);

                            string unencrypted = service.Decrypt(jsonstring);
                            T json = JsonConvert.DeserializeObject<T>(unencrypted);

                            if (!rows.TryAdd(id, json))
                            {
                                Console.WriteLine($"{id} could not be added to the dictionary.");
                            }
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine($"The file {file} requested could not be parsed according to this object.");
                        }
                    });
                }
            }
            else
            {
                Parallel.ForEach(files, file =>
                {
                    try
                    {
                        int id = int.Parse(Path.GetFileNameWithoutExtension(file));
                        var jsonstring = File.ReadAllText(file);
                        var json = JsonConvert.DeserializeObject<T>(jsonstring);

                        if (!rows.TryAdd(id, json))
                        {
                            Console.WriteLine($"{id} could not be added to the dictionary.");
                        }
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine($"The file {file} requested could not be parsed according to this object.");
                    }
                });
            }


            return rows;
        }

        /// <summary>
        /// Finds a single object based on the PK of the object.
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="JsonSerializationException">Thrown if the text could not be serialized.</exception>
        /// <exception cref="FileNotFoundException">ID could not be found in directory.</exception>
        public T Read(int id)
        {
            string path = Path.Combine(DirectoryPath, id.ToString() + extension);

            if (File.Exists(path))
            {
                string text = File.ReadAllText(path);

                if (encrypted)
                {
                    using (var service = new Cryptography(encryptionkey))
                    {
                        string unencrypted = service.Decrypt(text);
                        return JsonConvert.DeserializeObject<T>(unencrypted);
                    }
                }
                else
                {
                    return JsonConvert.DeserializeObject<T>(text);
                }
            }
            else
            {
                throw new FileNotFoundException("The ID in question could not be found by the system.", path);
            }
        }

        /// <summary>
        /// Serializes the object and either updates an existing file in the directory, or adds a new file.
        /// </summary>
        public void Write(int id, T value, bool overwrite = true)
        {
            string newfilepath = Path.Combine(DirectoryPath, id.ToString() + extension);

            if (!overwrite && File.Exists(newfilepath))
            {
                throw new UnauthorizedAccessException("The ID in question already has a file associated with it.");
            }

            string jsonserialized = JsonConvert.SerializeObject(value);
            string text = string.Empty;

            if (encrypted)
            {
                using (var service = new Cryptography(encryptionkey))
                {

                    text = service.Encrypt(jsonserialized);
                }
            }
            else
            {
                text = jsonserialized;
            }

            File.WriteAllText(newfilepath, text);
        }

        public void Write(IDictionary<int, T> values, bool overwrite = true)
        {
            if (encrypted)
            {
                using (var service = new Cryptography(encryptionkey))
                {
                    Parallel.ForEach(values, value =>
                    {
                        string newfilepath = Path.Combine(DirectoryPath, value.Key.ToString() + extension);
                        if (!overwrite && File.Exists(newfilepath))
                        {
                            throw new UnauthorizedAccessException("The ID in question already has a file associated with it.");
                        }

                        string jsonserialized = JsonConvert.SerializeObject(value.Value);
                        string text = service.Encrypt(jsonserialized);
                        File.WriteAllText(newfilepath, text);
                    });
                }
            }
            else
            {
                Parallel.ForEach(values, value =>
                {
                    string newfilepath = Path.Combine(DirectoryPath, value.Key.ToString() + extension);
                    if (!overwrite && File.Exists(newfilepath))
                    {
                        throw new UnauthorizedAccessException("The ID in question already has a file associated with it.");
                    }

                    string jsonserialized = JsonConvert.SerializeObject(value.Value);
                    File.WriteAllText(newfilepath, jsonserialized);
                });
            }
        }

        public void Delete(int id)
        {
            string deletepath = Path.Combine(DirectoryPath, id.ToString() + extension);
            File.Delete(deletepath);
        }
    }
}
