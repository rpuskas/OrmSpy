using FluentNHibernate.Mapping;

namespace OrmSpy
{
    public class Parent
    {
        public virtual int Id { get; set; }
    }

    public class ParentMap : ClassMap<Parent>
    {
        public ParentMap()
        {
            Id(x => x.Id).GeneratedBy.Increment();
        }
    }
}