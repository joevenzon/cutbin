using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CutBin
{
    public class CutProperty
    {
        public string name;
        public double value;
        public string units;
        public bool enabled;
    }

    public struct CutPropertyCache
    {
        public double speed;
        public double tooldiam;
        public double teeth;
        public double helix;
        public double d;
        public double r;
        public double Kp;
        public double E;
        public double stickout;
        public double youngs;
        public double wear;
        public double fpt;
    }

    public class Cut
    {
        public string _name;
        public string _guid;
        [XmlIgnore]
        Cut _parent;
        List<Cut> _children;
        public Dictionary<string,CutProperty> _properties;
        
        private static readonly CutProperty[] k_cutProperties = new CutProperty[]
        {
            new CutProperty { name = "Material Power Constant", value = 0.154, units = "HP/(in^3/min)", enabled = true },
            new CutProperty { name = "Power Efficiency", value = 0.75, units = "", enabled = true },
            new CutProperty { name = "Tool Diameter", value = 0.25, units = "in", enabled = true },
            new CutProperty { name = "Number of Teeth", value = 2, units = "", enabled = true },
            new CutProperty { name = "Helix Angle", value = 30, units = "deg", enabled = true },
            new CutProperty { name = "Stick out", value = 1.25, units = "in", enabled = true },
            new CutProperty { name = "Tool Youngs Modulus", value = 600, units = "Gpa", enabled = true },
            new CutProperty { name = "Tool Wear Factor", value = 1.2, units = "", enabled = true },
            new CutProperty { name = "Radial Width of Cut", value = 0.05, units = "in", enabled = true },
            new CutProperty { name = "Axial Depth of Cut", value = 0.1, units = "in", enabled = true },
            new CutProperty { name = "Feed Per Tooth", value = 0.001, units = "in/tooth", enabled = true },
            new CutProperty { name = "Cutting Speed", value = 1000, units = "ft/min", enabled = true },
        };
        private static readonly CutProperty[] k_computedProperties = new CutProperty[]
        {
            new CutProperty { name = "RPM" },
            new CutProperty { name = "Feed Rate", units = "in/min" },
            new CutProperty { name = "Material Removal Rate", units = "in^3/min" },
            new CutProperty { name = "Chip Thickness", units = "in" },
            new CutProperty { name = "Power at Tool", units = "HP" },
            new CutProperty { name = "Power at Motor", units = "HP" },
            new CutProperty { name = "Tangential Cutting Force", units = "lbf" },
            new CutProperty { name = "Maximum Force Vector", units = "lbf" },
            new CutProperty { name = "Deflection of Tool", units = "in" },
        };

        public Cut()
        {
            _name = "New cut";
            _guid = SanitizeGuid(Guid.NewGuid().ToString());
            _parent = null;
            _children = new List<Cut>();
            _properties = new Dictionary <string, CutProperty>();
        }

        string SanitizeGuid(string guid)
        {
            string newguid = guid.Replace("-", "_");
            return "_" + newguid;
        }

        public string Name { get { return _name; } set { _name = value; } }
        public List<Cut> Children { get {return _children; } }

        public Cut GetParent() { return _parent; }

        public void ClearParent()
        {
            _parent = null;
        }

        public void AddChild(Cut child)
        {
            _children.Add(child);
            child._parent = this;
        }

        public static List<CutProperty> GetCutPropertyDefinitions()
        {
            return k_cutProperties.ToList();
        }

        public static List<CutProperty> GetComputedPropertyDefinitions()
        {
            return k_computedProperties.ToList();
        }

        public bool PropertyEnabled(string name)
        {
            CutProperty property;
            if (!_properties.TryGetValue(name, out property))
            {
                return false;
            }
            else
            {
                return property.enabled;
            }
        }

        public void DisableProperty(string name)
        {
            CutProperty property;
            if (_properties.TryGetValue(name, out property))
            {
                property.enabled = false;
            }
        }

        public bool IsPropertyName(string name)
        {
            return k_cutProperties.Select(x => x.name == name).Count() > 0;
        }

        public void EnableAndSetProperty(string name, double value)
        {
            if (PropertyEnabled(name))
            {
                SetProperty(name, value);
            }
            else
            {
                Debug.Assert(IsPropertyName(name));

                CutProperty property;
                if (_properties.TryGetValue(name, out property))
                {
                    property.enabled = true;
                    property.value = value;
                }
                else
                {
                    property = new CutProperty();
                    property.name = name;
                    property.enabled = true;
                    property.value = value;
                    _properties.Add(name, property);
                }
            }
        }

        public bool SetProperty(string name, double value)
        {
            CutProperty property;
            if (!_properties.TryGetValue(name, out property) ||
                property.enabled == false)
            {
                return false;
            }
            else
            {
                property.value = value;
                return true;
            }
        }

        public double GetProperty(string name)
        {
            CutProperty property;
            if (!_properties.TryGetValue(name, out property) ||
                property.enabled == false)
            {
                if (_parent != null)
                    return _parent.GetProperty(name);
                else
                    return GetDefault(name);
            }
            else
            {
                return property.value;
            }
        }

        public double GetPropertyStrict(string name)
        {
            Debug.Assert(k_cutProperties.Where(x => x.name == name).Count() > 0, "Unknown property: " + name);
            return GetProperty(name);
        }

        public double GetDefault(string name)
        {
            return Array.Find(k_cutProperties, x => x.name == name).value;
        }

        public Cut GetChildByGuid(string guid)
        {
            foreach (Cut cut in _children)
            {
                if (cut._guid == guid)
                    return cut;
                else
                {
                    Cut result = cut.GetChildByGuid(guid);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        public CutPropertyCache GetCutProperties()
        {
            CutPropertyCache result = new CutPropertyCache();

            result.speed = GetPropertyStrict("Cutting Speed");
            result.tooldiam = GetPropertyStrict("Tool Diameter");
            result.teeth = GetPropertyStrict("Number of Teeth");
            result.helix = GetPropertyStrict("Helix Angle");
            result.d = GetPropertyStrict("Axial Depth of Cut");
            result.r = GetPropertyStrict("Radial Width of Cut");
            result.Kp = GetPropertyStrict("Material Power Constant");
            result.E = GetPropertyStrict("Power Efficiency");
            result.stickout = GetPropertyStrict("Stick out");
            result.youngs = GetPropertyStrict("Tool Youngs Modulus");
            result.wear = GetPropertyStrict("Tool Wear Factor");
            result.fpt = GetPropertyStrict("Feed Per Tooth");

            return result;
        }

        public double ComputeValue(string name)
        {
            Dictionary<string, double> results = new Dictionary<string, double>();

            CutPropertyCache prop = GetCutProperties();
            
            double rpm = 12 * prop.speed / (Math.PI * prop.tooldiam);
            double feedrate = rpm * prop.fpt * prop.teeth;
            double mrr = feedrate * prop.d * prop.r;
            double feedfactor = 0.417 * Math.Pow(prop.fpt, -0.197);
            double powertool = feedfactor * prop.Kp * prop.wear * mrr;
            double powermotor = powertool / prop.E;
            double Fc = powertool * 550 / (prop.speed /60);
            double Fmax = 1.11 * Fc;
            double comparedToSolid = 1.75;
            double deflection = (Fc * Math.Pow(prop.stickout, 3)) / (3 * prop.youngs * 145037 * comparedToSolid * Math.Pow(prop.tooldiam,4)/64);
            double engagementAngle = Math.Acos(1.0 - prop.r / (prop.tooldiam*0.5)) * (180 / Math.PI);
            double chipthicknessAngle = Math.Min(90, engagementAngle);
            double chipthickness = prop.fpt * Math.Sin(chipthicknessAngle * Math.PI / 180);

            results.Add("Feed Rate", feedrate);
            results.Add("RPM", rpm);
            results.Add("Material Removal Rate", mrr);
            results.Add("Power at Tool", powertool);
            results.Add("Power at Motor", powermotor);
            results.Add("Tangential Cutting Force", Fc);
            results.Add("Maximum Force Vector", Fmax);
            results.Add("Deflection of Tool", deflection);
            results.Add("Chip Thickness", chipthickness);

            return results[name];
        }

        static double wrap(double a, double b)
        {
            return a - b * Math.Floor(a / b);
        }

        public List<double> ComputeChipThicknesses(List<double> toolAngles)
        {
            CutPropertyCache prop = GetCutProperties();
            List<double> results = new List<double>();

            const double k_z_resolution = 0.001;

            double toolradius = prop.tooldiam / 2;

            foreach (double angle in toolAngles)
            {
                double total = 0;

                for (double tooth = 0; tooth < prop.teeth; tooth++)
                {
                    double toothStartAngle = angle + 360*(tooth/prop.teeth);
                    double toothResult = 0;

                    for (double z = prop.d; z >= 0; z -= k_z_resolution)
                    //for (double z = 0; z <= prop.d; z += k_z_resolution)
                    {
                        double toothAngle = toothStartAngle + (180/Math.PI)*(-z)*Math.Tan(prop.helix*(Math.PI/180))/toolradius;
                        toothAngle = wrap(toothAngle, 360);

                        double engagementAngle = Math.Acos(1.0 - prop.r / toolradius)*(180/Math.PI);
                        double engagementStartAngle = 180 - engagementAngle;

                        double chipThickness = 0;

                        if (toothAngle > engagementStartAngle && toothAngle < 180)
                        {
                            chipThickness = prop.fpt * Math.Sin(toothAngle * (Math.PI / 180));
                        }

                        toothResult += chipThickness;
                    }

                    total += toothResult;
                }

                results.Add(total);
            }

            return results;
        }

        public List<double> ComputeMaterialRemoved(List<double> toolAngles)
        {
            CutPropertyCache prop = GetCutProperties();
            List<double> results = new List<double>();

            const double k_z_resolution = 0.001;

            double toolradius = prop.tooldiam / 2;

            double angleStep = toolAngles[1] - toolAngles[0];

            foreach (double angle in toolAngles)
            {
                double total = 0;

                for (double tooth = 0; tooth < prop.teeth; tooth++)
                {
                    double toothStartAngle = angle + 360 * (tooth / prop.teeth);
                    double toothResult = 0;

                    for (double z = prop.d; z >= 0; z -= k_z_resolution)
                    //for (double z = 0; z <= prop.d; z += k_z_resolution)
                    {
                        double toothAngle = toothStartAngle + (180 / Math.PI) * (-z) * Math.Tan(prop.helix * (Math.PI / 180)) / toolradius;
                        toothAngle = wrap(toothAngle, 360);

                        double engagementAngle = Math.Acos(1.0 - prop.r / toolradius) * (180 / Math.PI);
                        double engagementStartAngle = 180 - engagementAngle;

                        double chipThickness = 0;

                        if (toothAngle > engagementStartAngle && toothAngle < 180)
                        {
                            chipThickness = prop.fpt * Math.Sin(toothAngle * (Math.PI / 180));
                        }

                        double materialRemoved = chipThickness * k_z_resolution * Math.Sin(angleStep * Math.PI / 180) * toolradius;

                        toothResult += materialRemoved;
                    }

                    total += toothResult;
                }
                
                results.Add(total);
            }

            return results;
        }

        public List<double> ComputeCuttingForces(List<double> toolAngles)
        {
            CutPropertyCache prop = GetCutProperties();
            List<double> results = new List<double>();

            const double k_z_resolution = 0.001;

            double toolradius = prop.tooldiam / 2;
            
            double angleStep = toolAngles[1] - toolAngles[0];

            foreach (double angle in toolAngles)
            {
                double total = 0;

                for (double tooth = 0; tooth < prop.teeth; tooth++)
                {
                    double toothStartAngle = angle + 360 * (tooth / prop.teeth);
                    double toothResult = 0;

                    for (double z = prop.d; z >= 0; z -= k_z_resolution)
                    //for (double z = 0; z <= prop.d; z += k_z_resolution)
                    {
                        double toothAngle = toothStartAngle + (180 / Math.PI) * (-z) * Math.Tan(prop.helix * (Math.PI / 180)) / toolradius;
                        toothAngle = wrap(toothAngle, 360);

                        double engagementAngle = Math.Acos(1.0 - prop.r / toolradius) * (180 / Math.PI);
                        double engagementStartAngle = 180 - engagementAngle;

                        double chipThickness = 0;

                        if (toothAngle > engagementStartAngle && toothAngle < 180)
                        {
                            chipThickness = prop.fpt * Math.Sin(toothAngle * (Math.PI / 180));
                        }

                        double materialRemoved = chipThickness * k_z_resolution * Math.Sin(angleStep * Math.PI / 180) * toolradius;

                        toothResult += materialRemoved;
                    }

                    total += toothResult;
                }

                double rpm = 12 * prop.speed / (Math.PI * prop.tooldiam);
                double mrr = total*rpm;
                double feedfactor = 0.417 * Math.Pow(prop.fpt, -0.197);
                double powertool = feedfactor * prop.Kp * prop.wear * mrr;
                double powermotor = powertool / prop.E;
                double Fc = powertool * 550 / (prop.speed / 60);

                results.Add(Fc);
            }

            return results;
        }

        public bool HasChild(Cut cut)
        {
            return (GetChildByGuid(cut._guid) != null);
        }

        public void AfterLoad()
        {
            foreach (Cut child in _children)
            {
                child._parent = this;
                child.AfterLoad();
            }
        }
    }

    public class CutDB
    {
        public List<Cut> Cuts;

        public CutDB()
        {
            Cuts = new List<Cut>();
        }

        public Cut GetCutByGuid(string guid)
        {
            foreach (Cut cut in Cuts)
            {
                if (cut._guid == guid)
                    return cut;
                else
                {
                    Cut result = cut.GetChildByGuid(guid);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        public void Delete(Cut cut)
        {
            Cut parent = cut.GetParent();
            if (parent == null)
            {
                Cuts.Remove(cut);
            }
            else
            {
                parent.Children.Remove(cut);
            }

            // prevent dangling reference
            cut.ClearParent();
        }

        public bool Reparent(Cut cut, Cut parent)
        {
            bool result = false;

            // prevent cycles
            if (!cut.HasChild(parent))
            {
                Delete(cut);
                parent.AddChild(cut);
                result = true;
            }

            return result;
        }

        public void AfterLoad()
        {
            // fixup parent links
            foreach (Cut cut in Cuts)
            {
                cut.AfterLoad();
            }
        }
    }

}
