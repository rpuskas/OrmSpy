using FluentNHibernate.Mapping;

namespace OrmSpy
{
    public class Marriage
    {
        public virtual int Id { get; set; }
        public virtual Man Man { get; set; }
        public virtual Woman Woman { get; set; }
    }

    public class Man
    {
        public virtual int Id { get; set; }
        public virtual Properties Properties { get; set; }

        public Man()
        {
            Properties = new Properties{Age = 21};
        }
    }
    
    public class Woman
    {
        public virtual int Id { get; set; }
        public virtual Properties Properties { get; set; }

        public Woman()
        {
            Properties = new Properties{Age = 18};
        }
    }

    public class Properties
    {
        public virtual int Id { get; set; }
        public virtual int Age { get; set; }
        public virtual int Weight { get; set; }
        public virtual int Height { get; set; }
    }

    public class PropertiesMap : ClassMap<Properties>
    {
        public PropertiesMap()
        {
            Id(x => x.Id).GeneratedBy.Increment();
            Map(x => x.Age);
        }
    }

    public class ManMap : ClassMap<Man>
    {
        public ManMap()
        {
            Id(x => x.Id).GeneratedBy.Increment();
            References(x => x.Properties).Cascade.All();
        }
    }

    public class WomanMap : ClassMap<Woman>
    {
        public WomanMap()
        {
            Id(x => x.Id).GeneratedBy.Increment();
            References(x => x.Properties).Cascade.All();
        }
    }

    public class MarriageMap : ClassMap<Marriage>
    {
        public MarriageMap()
        {
            Id(x => x.Id).GeneratedBy.Increment();
            References(x => x.Man);
            References(x => x.Woman);
        }
    }
}