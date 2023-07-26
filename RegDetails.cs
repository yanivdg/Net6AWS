[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ConsoleAppDBCloud
{
public class RegDetails
{
       public long ID { get; set; }
        public string FullName { get; set; }
        public string user { get; set; }
        public string password { get; set; }
        public string token { get; set; }
        public DateTime Validate { get; set; }
        public class EmpDBContext : DbContext
        {
            public EmpDBContext()
            { }
            public DbSet<RegDetails> Regs { get; set; }
        }
}
}
