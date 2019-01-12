namespace StockDoctor.Core.Attributes {


    [System.AttributeUsage(System.AttributeTargets.Property, 
                        AllowMultiple = true)  // Multiuse attribute.  
    ]  
    public class NotConsumed : System.Attribute  
    {  

    }

}