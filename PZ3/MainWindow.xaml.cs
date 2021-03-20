using RG_PSI_PZ3.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Point = System.Windows.Point;


namespace PZ3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public double noviX, noviY;
        private int zoom = 40;
        private int trenutan_zoom = 1;
        private double minX = 19.793909;
        private double maxX = 19.894459;
        private double minY = 45.2325;
        private double maxY = 45.277031;
        ToolTip tooltip = new ToolTip();
        private Point pocetak = new Point();
        private Point diffOffset = new Point();
        private double prevOffset = 0;
        List<PowerEntity> listasvega = new List<PowerEntity>();
        List<LineEntity> linentitylist = new List<LineEntity>();
        private Dictionary<long, GeometryModel3D> listpowerentity3d = new Dictionary<long, GeometryModel3D>();
        private Dictionary<long, GeometryModel3D> listlines3d = new Dictionary<long, GeometryModel3D>();
        private List<Tuple<long, SolidColorBrush>> obojeni = new List<Tuple<long, SolidColorBrush>>();



        public MainWindow()
        {
            InitializeComponent();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Geographic.xml");

            XmlNodeList nodeList;

            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");
            foreach (XmlNode node in nodeList)
            {
                SubstationEntity sub = new SubstationEntity();

                sub.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                sub.Name = node.SelectSingleNode("Name").InnerText;
                sub.X = double.Parse(node.SelectSingleNode("X").InnerText);
                sub.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                ToLatLon(sub.X, sub.Y, 34, out noviY, out noviX);
                sub.X = noviX;
                sub.Y = noviY;

                if (sub.X >= minX && sub.X <= maxX && sub.Y >= minY && sub.Y <= maxY)
                {
                    listasvega.Add(sub);
                }
            }

            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");
            foreach (XmlNode node in nodeList)
            {
                NodeEntity nodeEntity = new NodeEntity();

                nodeEntity.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                nodeEntity.Name = node.SelectSingleNode("Name").InnerText;
                nodeEntity.X = double.Parse(node.SelectSingleNode("X").InnerText);
                nodeEntity.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                ToLatLon(nodeEntity.X, nodeEntity.Y, 34, out noviY, out noviX);
                nodeEntity.X = noviX;
                nodeEntity.Y = noviY;

                if (nodeEntity.X >= minX && nodeEntity.X <= maxX && nodeEntity.Y >= minY && nodeEntity.Y <= maxY)
                {
                    listasvega.Add(nodeEntity);
                }
            }


            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");
            foreach (XmlNode node in nodeList)
            {
                SwitchEntity switchEntity = new SwitchEntity();

                switchEntity.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                switchEntity.Name = node.SelectSingleNode("Name").InnerText;
                switchEntity.X = double.Parse(node.SelectSingleNode("X").InnerText);
                switchEntity.Y = double.Parse(node.SelectSingleNode("Y").InnerText);
                switchEntity.Status = node.SelectSingleNode("Status").InnerText;

                ToLatLon(switchEntity.X, switchEntity.Y, 34, out noviY, out noviX);
                switchEntity.X = noviX;
                switchEntity.Y = noviY;

                if (switchEntity.X >= minX && switchEntity.X <= maxX && switchEntity.Y >= minY && switchEntity.Y <= maxY)
                {
                    listasvega.Add(switchEntity);
                }
            }

            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity");
            foreach (XmlNode node in nodeList)
            {
                LineEntity l = new LineEntity();
                l.Vertices = new List<Point>();

                l.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                l.Name = node.SelectSingleNode("Name").InnerText;
                if (node.SelectSingleNode("IsUnderground").InnerText.Equals("true"))
                {
                    l.IsUnderground = true;
                }
                else
                {
                    l.IsUnderground = false;
                }
                l.R = float.Parse(node.SelectSingleNode("R").InnerText);
                l.ConductorMaterial = node.SelectSingleNode("ConductorMaterial").InnerText;
                l.LineType = node.SelectSingleNode("LineType").InnerText;
                l.ThermalConstantHeat = long.Parse(node.SelectSingleNode("ThermalConstantHeat").InnerText);
                l.FirstEnd = long.Parse(node.SelectSingleNode("FirstEnd").InnerText);
                l.SecondEnd = long.Parse(node.SelectSingleNode("SecondEnd").InnerText);


                bool exists = false;
                foreach (LineEntity line in linentitylist)
                {
                    if ((line.FirstEnd == l.FirstEnd || line.FirstEnd == l.SecondEnd) &&
                        (line.SecondEnd == l.FirstEnd || line.SecondEnd == l.SecondEnd)) //provera da se odbace nepozeljne linije
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                {
                    continue;
                }

                foreach (XmlNode pointNode in node.ChildNodes[9].ChildNodes)
                {
                    Point point = new Point();

                    point.X = double.Parse(pointNode.SelectSingleNode("X").InnerText);
                    point.Y = double.Parse(pointNode.SelectSingleNode("Y").InnerText);

                    ToLatLon(point.X, point.Y, 34, out noviY, out noviX);

                    if (noviX >= minX && noviX <= maxX && noviY >= minY && noviY <= maxY)
                    {
                        l.Vertices.Add(new Point(noviX, noviY));
                    }
                }

                linentitylist.Add(l);
            }

            foreach (PowerEntity p in listasvega)
            {
                PowertoViewPort(p);
            }

            foreach (LineEntity l1 in linentitylist)
            {
                LineToViewPort(l1);
            }


        }


        private void PowertoViewPort(PowerEntity powerEntity)
        {
            double y = Scale(powerEntity.Y, minY, maxY, -1, 1);
            double x = Scale(powerEntity.X, minX, maxX, -1.5, 1.5);

            GeometryModel3D node3d = new GeometryModel3D();
            MeshGeometry3D mesh3d = new MeshGeometry3D();

            mesh3d.Positions.Add(new Point3D(x, y, 0.03)); // 0.03 velicina elementa
            mesh3d.Positions.Add(new Point3D(x + 0.01, y, 0));
            mesh3d.Positions.Add(new Point3D(x, y + 0.01, 0));
            mesh3d.Positions.Add(new Point3D(x + 0.01, y + 0.01, 0));
            mesh3d.Positions.Add(new Point3D(x, y, 0.01 * 1.5)); //1.5
            mesh3d.Positions.Add(new Point3D(x + 0.01, y, 0.01 * 1.5));
            mesh3d.Positions.Add(new Point3D(x, y + 0.01, 0.01 * 1.5));
            mesh3d.Positions.Add(new Point3D(x + 0.01, y + 0.01, 0.01 * 1.5));

            mesh3d.TriangleIndices = new Int32Collection() { 0, 3, 1, 0, 2, 3, 1, 3, 7, 1, 7, 5, 4, 5, 7, 4, 7, 6, 0, 4, 6, 0, 6, 2, 2, 6, 7, 2, 7, 3, 0, 1, 5, 0, 5, 4 };

            node3d.Geometry = mesh3d;


            if (powerEntity.GetType().Equals(typeof(NodeEntity)))
            {
                node3d.Material = new DiffuseMaterial(Brushes.Magenta);
            }
            else if (powerEntity.GetType().Equals(typeof(SwitchEntity)))
            {
                SwitchEntity sw = (SwitchEntity)powerEntity;
                if (sw.Status == "Open")
                {
                    node3d.Material = new DiffuseMaterial(Brushes.LawnGreen);
                }
                else
                {
                    node3d.Material = new DiffuseMaterial(Brushes.IndianRed);
                }
            }
            else if (powerEntity.GetType().Equals(typeof(SubstationEntity)))
            {
                node3d.Material = new DiffuseMaterial(Brushes.CornflowerBlue);
            }
            else
            {
                Console.WriteLine();
            }


            foreach (var node3dtemp in listpowerentity3d.Values)
            {
                var mesh3dtemp = (MeshGeometry3D)node3d.Geometry;
                double height = node3dtemp.Bounds.SizeZ;
                double emptySpace = 0.01;
                double amountToRise = height + emptySpace;

                while (mesh3dtemp.Bounds.IntersectsWith(node3dtemp.Bounds))
                {
                    for (int i = 0; i < mesh3dtemp.Positions.Count; i++)
                    {
                        var currPos = mesh3dtemp.Positions[i];
                        mesh3dtemp.Positions[i] = new Point3D(currPos.X, currPos.Y, currPos.Z + amountToRise);
                    }
                }

                node3d.Geometry = mesh3dtemp;
                node3d.SetValue(System.Windows.FrameworkElement.TagProperty, powerEntity);
            }


            listpowerentity3d.Add(powerEntity.Id, node3d);
            model3d.Children.Add(node3d);
            //nodes.Add(powerEntity.Item2.Id, node);
        }
        private void LineToViewPort(LineEntity lineEntity)
        {
            List<Point> v = lineEntity.Vertices;
            var geometryLines = new List<GeometryModel3D>();
            var model = new GeometryModel3D();
            var mesh3d = new MeshGeometry3D();
            var points = new Point3DCollection();


            for (int i = 0; i < v.Count - 1; ++i)
            {
                var model1 = new GeometryModel3D();

                double startx;
                double starty;

                startx = Scale(v[i].X, minX, maxX, -1.5, 1.5);
                starty = Scale(v[i].Y, minY, maxY, -1, 1);
                double endx = Scale(v[i + 1].X, minX, maxX, -1.5, 1.5);
                double endy = Scale(v[i + 1].Y, minY, maxY, -1, 1);

                Point3D start = new Point3D();
                start.X = startx;
                start.Y = starty;
                start.Z = 0.0009;
                Point3D end = new Point3D();
                end.X = endx;
                end.Y = endy;
                end.Z = 0.0009;

                var vecDiff = end - start;

                var nVector = Vector3D.CrossProduct(vecDiff, new Vector3D(0, 0, 1));
                
                nVector = Vector3D.Divide(nVector, nVector.Length);
                nVector = Vector3D.Multiply(nVector, 0.0009);

                points.Add(start - nVector);
                points.Add(start + nVector);
                points.Add(end - nVector);
                points.Add(end + nVector);

            }
            mesh3d.Positions = points;

            for (int i = 0; i < mesh3d.Positions.Count - 2; i++)
            {
                mesh3d.TriangleIndices.Add(i);
                mesh3d.TriangleIndices.Add(i + 2);
                mesh3d.TriangleIndices.Add(i + 1);
                mesh3d.TriangleIndices.Add(i);
                mesh3d.TriangleIndices.Add(i + 1);
                mesh3d.TriangleIndices.Add(i + 2);
            }

            model.Material = new DiffuseMaterial(Brushes.Purple);
            model.Geometry = mesh3d;


            model.SetValue(System.Windows.FrameworkElement.TagProperty, lineEntity);

            listlines3d.Add(lineEntity.Id, model);

            model3d.Children.Add(model);

        }

        public static double Scale(double val, double min, double max, double minS, double maxS)
        {
            double temp_p = 0;
            temp_p = minS + ((val - min) / (max - min) * (maxS - minS));
            return temp_p;
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var p = e.MouseDevice.GetPosition(this);
            double scaleX = 1;
            double scaleY = 1;
            Point temp_p = new Point();
            temp_p.X = scaleX;
            temp_p.Y = scaleY;
            if (e.Delta > 0 && trenutan_zoom < zoom)
            {
                temp_p.X = scale.ScaleX + 0.1;
                temp_p.Y = scale.ScaleY + 0.1;
                scale.ScaleX = temp_p.X;
                scale.ScaleY = temp_p.Y;
                trenutan_zoom++;
            }
            else if (e.Delta <= 0 && trenutan_zoom > -5)
            {
                temp_p.X = scale.ScaleX - 0.1;
                temp_p.Y = scale.ScaleY - 0.1;
                scale.ScaleX = temp_p.X;
                scale.ScaleY = temp_p.Y;
                trenutan_zoom--;
            }
        }
        private void Viewport_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewport.CaptureMouse();
            pocetak = e.GetPosition(this);
            diffOffset.X = translate.OffsetX;
            diffOffset.Y = translate.OffsetY;
        }
        private void Viewport_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            viewport.ReleaseMouseCapture();
        }
        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewport.CaptureMouse();
            pocetak = e.GetPosition(this);
            diffOffset.X = translate.OffsetX;
            diffOffset.Y = translate.OffsetY;

            Point mousePosition = e.GetPosition(viewport);
            Point3D testPoint = new Point3D(mousePosition.X, mousePosition.Y, 0);
            Vector3D testDirection = new Vector3D(mousePosition.X, mousePosition.Y, 4);

            PointHitTestParameters pointparams = new PointHitTestParameters(mousePosition);
            RayHitTestParameters rayparams = new RayHitTestParameters(testPoint, testDirection);

            VisualTreeHelper.HitTest(viewport, null, HTResult2, pointparams);

        }

        private HitTestResultBehavior HTResult(HitTestResult rawResult)
        {
            RayMeshGeometry3DHitTestResult mesh_result =
                  rawResult as RayMeshGeometry3DHitTestResult;

            if (mesh_result != null && tooltip != null)
            {
                var entity = mesh_result.ModelHit.GetValue(TagProperty);
                if (entity is PowerEntity powerentity)
                {
                    tooltip.IsOpen = true;

                    string mbText = powerentity.ToString();
                    foreach (PowerEntity pw in listasvega)
                    {
                        if (pw == powerentity)
                        {
                            string str = Reverse(powerentity.GetType().ToString().Substring(17));
                            str = str.Substring(6);
                            str = Reverse(str);
                            tooltip.Content = (str.ToUpper() + "\nId: " + powerentity.Id + "\nName: " + powerentity.Name);
                            if (powerentity.GetType().Equals(typeof(SwitchEntity)))
                                tooltip.Content += "\nStatus:\t" + ((SwitchEntity)powerentity).Status;
                            tooltip.StaysOpen = true;
                        }
                    }
                }
                else if (entity is LineEntity lineentity)
                {
                    tooltip.IsOpen = true;

                    string mbText = lineentity.ToString();
                    foreach (LineEntity lajn in linentitylist)
                    {
                        if (lajn == lineentity)
                        {
                            string str = Reverse(lineentity.GetType().ToString().Substring(17));
                            str = str.Substring(6);
                            str = Reverse(str);
                            tooltip.Content = "LINE" + "\nID: " + lajn.Id +
                                "\nLine type: " + lajn.LineType + "\nName: " + lajn.Name + "\nIs Underground: " + lajn.IsUnderground
                                + "\nThermalConstantHeat: " + lajn.ThermalConstantHeat + "\nR: " + lajn.R + "\nConductor Material: " + lajn.ConductorMaterial;
                            tooltip.StaysOpen = true;
                        }
                    }

                }
                else
                {
                    tooltip.IsOpen = false;
                }

            }
            return HitTestResultBehavior.Stop;
        }
        private HitTestResultBehavior HTResult2(HitTestResult rawResult)
        {
            if (rawResult is RayHitTestResult rayResult)
            {
                var tagProp = rayResult.ModelHit.GetValue(TagProperty);

                if (tagProp is LineEntity lineEntity)
                {
                    // OnClick(lineEntity);
                    foreach (long key in listlines3d.Keys)
                    {
                        if (listlines3d[key] == ((GeometryModel3D)rayResult.ModelHit))
                        {
                            if (obojeni.Count != 0)
                            {
                                try
                                {
                                    listlines3d[(int)obojeni[0].Item1].Material = new DiffuseMaterial(obojeni[0].Item2);
                                    listpowerentity3d[(int)obojeni[1].Item1].Material = new DiffuseMaterial(obojeni[1].Item2);
                                    listpowerentity3d[(int)obojeni[2].Item1].Material = new DiffuseMaterial(obojeni[2].Item2);
                                }
                                catch
                                {

                                }
                            }

                            obojeni.Clear();

                            var modelhit = (GeometryModel3D)rayResult.ModelHit;
                            LineEntity lajn = new LineEntity();
                            foreach (LineEntity l_temp in linentitylist)
                            {
                                if (l_temp.Id == key)
                                {
                                    lajn = l_temp;
                                    break;
                                }
                            }

                            obojeni.Add(new Tuple<long, SolidColorBrush>(key, Brushes.Purple));



                            foreach (PowerEntity pw in listasvega)
                            {
                                if (lajn.FirstEnd == pw.Id)
                                {
                                    if (pw.GetType().Equals(typeof(NodeEntity)))
                                    {
                                        obojeni.Add(new Tuple<long, SolidColorBrush>(lineEntity.FirstEnd, Brushes.Magenta));
                                        break;
                                    }
                                    else if (pw.GetType().Equals(typeof(SubstationEntity)))
                                    {
                                        obojeni.Add(new Tuple<long, SolidColorBrush>(lineEntity.FirstEnd, Brushes.LawnGreen));
                                        break;

                                    }
                                    else if (pw.GetType().Equals(typeof(SwitchEntity)))
                                    {
                                        obojeni.Add(new Tuple<long, SolidColorBrush>(lineEntity.FirstEnd, Brushes.CornflowerBlue));
                                        break;
                                    }

                                }
                            }

                            foreach (PowerEntity pw in listasvega)
                            {
                                if (lajn.SecondEnd == pw.Id)
                                {
                                    if (pw.GetType().Equals(typeof(NodeEntity)))
                                    {
                                        obojeni.Add(new Tuple<long, SolidColorBrush>(lineEntity.SecondEnd, Brushes.Magenta));
                                        break;
                                    }
                                    else if (pw.GetType().Equals(typeof(SubstationEntity)))
                                    {
                                        obojeni.Add(new Tuple<long, SolidColorBrush>(lineEntity.SecondEnd, Brushes.LawnGreen));
                                        break;

                                    }
                                    else if (pw.GetType().Equals(typeof(SwitchEntity)))
                                    {
                                        obojeni.Add(new Tuple<long, SolidColorBrush>(lineEntity.SecondEnd, Brushes.CornflowerBlue));
                                        break;
                                    }

                                }
                            }

                            try
                            {
                                listlines3d[key].Material = new DiffuseMaterial(Brushes.Yellow);
                                listpowerentity3d[lineEntity.FirstEnd].Material = new DiffuseMaterial(Brushes.Yellow);
                                listpowerentity3d[lineEntity.SecondEnd].Material = new DiffuseMaterial(Brushes.Yellow);
                            }
                            catch (Exception e)
                            {

                            }
                        }
                    }
                }
            }

            return HitTestResultBehavior.Stop;
        }
        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            viewport.ReleaseMouseCapture();
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {

            if (viewport.IsMouseCaptured)
            {
                Point kraj = e.GetPosition(this);
                double offsetX = kraj.X - pocetak.X;
                double offsetY = kraj.Y - pocetak.Y;
                double width = Width;
                double height = Height;
                double translateX = (offsetX * 100) / width;
                double translateY = -(offsetY * 100) / height;

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    translate.OffsetX = diffOffset.X + (translateX / (100 * scale.ScaleX));
                    translate.OffsetY = diffOffset.Y + (translateY / (100 * scale.ScaleX));
                }

                if (e.RightButton == MouseButtonState.Pressed)
                {

                    rotateY.Angle = (rotateY.Angle + translateX) % 360;
                    double rotationXAngle = rotateX.Angle + translateY % 360;
                    if (rotationXAngle > -100 && rotationXAngle < 65)
                    {
                        rotateX.Angle = rotationXAngle;
                    }

                    pocetak = kraj;
                }

                if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    double rotOff = offsetY > prevOffset ? translateY : -translateY;
                    rotate.Angle = (rotate.Angle + rotOff / 10) % 360;
                }

                prevOffset = offsetY;
            }


            Point mousePosition = e.GetPosition(viewport);
            Point3D testPoint = new Point3D(mousePosition.X, mousePosition.Y, 0);
            Vector3D testDirection = new Vector3D(mousePosition.X, mousePosition.Y, 4);

            PointHitTestParameters pointparams = new PointHitTestParameters(mousePosition);
            RayHitTestParameters rayparams = new RayHitTestParameters(testPoint, testDirection);

            VisualTreeHelper.HitTest(viewport, null, HTResult, pointparams);

        }
        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        private void viewport_MouseMiddleButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewport.CaptureMouse();
            pocetak = e.GetPosition(this);
            diffOffset.X = translate.OffsetX;
            diffOffset.Y = translate.OffsetY;
        }
        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                viewport_MouseMiddleButtonDown(sender, e);
            }
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            viewport.ReleaseMouseCapture();
        }

        public static void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }
    }
}
