using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DynamicLinqWhereAfterToArray
{
    class Program
    {
        private MyContext _context;
        public const string ConnectionString = @"ENTER!!!";

        static void Main(string[] args)
        {
            var now = DateTime.Now;
            Console.WriteLine("Application started.");

            var program = new Program();
            program.Run().Wait();

            Console.WriteLine($"Application ended in {(DateTime.Now - now)}.");

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        private async Task Run()
        {
            this._context = new MyContext();
            await this._context.Database.MigrateAsync();
            await this.GenerateData(100);

            //Option 1
            //var data0 = await this._context.Set(typeof(Entity1)).Where("Id<10").ToArrayAsync();        //Works
            //var data1 = await this._context.Set(typeof(Entity1)).ToDynamicArrayAsync();        //Works
            //var data2 = data1.AsQueryable().Where("Id<10").FirstOrDefault();          //Doesn't work.

            //Option 2
            //data0 = await this._context.Set(typeof(Entity1)).Where("Id<10").ToDynamicArrayAsync<object>();        //Works
            //data1 = await this._context.Set(typeof(Entity1)).ToDynamicArrayAsync();        //Works
            //data2 = data1.AsQueryable().Where("Id<10").FirstOrDefault();          //Doesn't work.

            //Option 3.1
            var data0 = await this._context.Set(typeof(Entity1)).Where("Id<10").ToDynamicArrayAsync();     //Works
            var data1 = await this._context.Set(typeof(Entity1)).ToDynamicArrayAsync();                             //Works
            var data2 = data1.AsQueryable().Where("Id<10").FirstOrDefault();                                 //Doesn't work: One or more errors occurred. (Operator '<' incompatible with operand types 'Object' and 'Int32')

            //Option 3.2
            data0 = await this._context.Set(typeof(Entity1)).Where("Id<10").ToDynamicArrayAsync();      //Works
            data1 = await this._context.Set(typeof(Entity1)).ToDynamicArrayAsync();                              //Works
            data2 = data1.AsQueryable().Where("Url=@0", "Test").FirstOrDefault();               //Doesn't work: One or more errors occurred. (Target object is not an ExpandoObject)

        }

        private async Task GenerateData(int iterations)
        {
            var r = new Random();
            for (var i = 0; i < iterations; i++)
            {
                this._context.Entity1.Add(new Entity1
                {
                    Rating = r.Next(1, 10),
                    Url = Extensions.GenerateRandomString(100)
                });
                this._context.Entity2.Add(new Entity2
                {
                    Rating2 = r.Next(1, 10),
                    Url2 = Extensions.GenerateRandomString(100)
                });
            }

            await this._context.SaveChangesAsync();
        }
    }

    public class MyContext : DbContext
    {
        public DbSet<Entity1> Entity1 { get; set; }
        public DbSet<Entity2> Entity2 { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Program.ConnectionString);
        }
    }

    public class Entity1
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Url { get; set; }
        public int Rating { get; set; }
    }

    public class Entity2
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Url2 { get; set; }
        public int Rating2 { get; set; }
    }

    public class Extensions
    {
        private static string UpperCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static string LowerCharacters = "abcdefghijklmnoprstuvzxyqw";
        private static string NumberCharacters = "0123456789";
        private static string SpecialCharacters = ",.-_!#$%&/()=";

        /// <summary>
        /// Generates the random string.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="chars">The chars.</param>
        /// <returns>System.String.</returns>
        public static string GenerateRandomString(int length, string chars = null)
        {
            var random = new Random();

            return new string(Enumerable.Repeat(chars ?? UpperCharacters + LowerCharacters + NumberCharacters + SpecialCharacters, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public static class ProgramExtensions
    {
        public static IQueryable<dynamic> Set(this MyContext context, Type T)
        {
            // Get the generic type definition
            MethodInfo method = typeof(DbContext).GetMethod(nameof(DbContext.Set), BindingFlags.Public | BindingFlags.Instance);

            // Build a method with the specific type argument you're interested in
            method = method.MakeGenericMethod(T);

            var methodResult = method.Invoke(context, null);
            return (IQueryable<dynamic>)methodResult;       //Before EF Core 2.1.8 we use 2.1.3, which allow type casting to DbSet<dynamic>.
        }
    }
}
