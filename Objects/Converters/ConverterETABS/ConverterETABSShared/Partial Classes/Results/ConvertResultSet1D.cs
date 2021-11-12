﻿using ETABSv1;
using Objects.Structural.Geometry;
using Objects.Structural.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public ResultSet1D FrameResultSet1dToSpeckle(string elementName)
        {
            List<Result1D> results = new List<Result1D>();

            SetLoadCombinationsForResults();

            // Reference variables for ETABS API
            int numberOfResults = 0;
            string[] obj, elm, loadCase, stepType;
            obj = elm = loadCase = stepType = new string[1];
            float[] objSta, elmSta, stepNum, p, v2, v3, t, m2, m3;
            objSta = elmSta = stepNum = p = v2 = v3 = t = m2 = m3 = new float[1];

            //Model.Results.FrameForce(elementName, eItemTypeElm.ObjectElm, ref numberOfResults, ref obj, ref objSta, ref elm, ref elmSta, ref loadCase, ref stepType, ref stepNum, ref p, ref v2, ref v3, ref t, ref m2, ref m3);

            //// Value used to normalized output station of forces between 0 and 1
            //var lengthOf1dElement = objSta.Max();

            //for (int i = 0; i < numberOfResults; i++)
            //{
            //    Result1D result = new Result1D()
            //    {
            //        element = new Element1D() { name = elementName }, // simple new Element1D until conversion class is created
            //        position = objSta[i] / lengthOf1dElement,
            //        permutation = loadCase[i],
            //        dispX = 0, // values eventually populated by element.Node.{displacements}
            //        dispY = 0, // values eventually populated by element.Node.{displacements}
            //        dispZ = 0, // values eventually populated by element.Node.{displacements}
            //        rotXX = 0, // values eventually populated by element.Node.{displacements}
            //        rotYY = 0, // values eventually populated by element.Node.{displacements}
            //        rotZZ = 0, // values eventually populated by element.Node.{displacements}
            //        forceX = v3[i],
            //        forceY = v2[i],
            //        forceZ = p[i],
            //        momentXX = m3[i],
            //        momentYY = m2[i],
            //        momentZZ = t[i],
            //        axialStress = 0, // values eventually populated when element1d.section values are available
            //        shearStressY = 0, // values eventually populated when element1d.section values are available
            //        shearStressZ = 0, // values eventually populated when element1d.section values are available
            //        bendingStressYPos = 0, // values eventually populated when element1d.section values are available
            //        bendingStressYNeg = 0, // values eventually populated when element1d.section values are available
            //        bendingStressZPos = 0, // values eventually populated when element1d.section values are available
            //        bendingStressZNeg = 0, // values eventually populated when element1d.section values are available
            //        combinedStressMax = 0, // values eventually populated when element1d.section values are available
            //        combinedStressMin = 0 // values eventually populated when element1d.section values are available
            //    };

            //    results.Add(result);
            //}

            return new ResultSet1D() { results1D = results };



        }

        public ResultSet1D PierResultSet1dToSpeckle(string elementName)
        {
            List<Result1D> results = new List<Result1D>();

            SetLoadCombinationsForResults();

            // Reference variables for ETABS API
            int numberOfResults = 0;
            string[] storyName, pierName, loadCase, location;
            storyName = pierName = loadCase = location = new string[1];
            float[] p, v2, v3, t, m2, m3;
            p = v2 = v3 = t = m2 = m3 = new float[1];

            //Model.Results.PierForce(ref numberOfResults, ref storyName, ref pierName, ref loadCase, ref location, ref p, ref v2, ref v3, ref t, ref m2, ref m3);

            //for (int i = 0; i < numberOfResults; i++)
            //{
            //    if(pierName[i] == elementName)
            //    {
            //        Result1D result = new Result1D()
            //        {
            //            element = new Element1D() { name = elementName }, // simple new Element1D until conversion class is created
            //            position = 0,
            //            description = "Location: " + location[i] + " - " + storyName[i],
            //            permutation = loadCase[i],
            //            dispX = 0, // values eventually populated by element.Node.{displacements}
            //            dispY = 0, // values eventually populated by element.Node.{displacements}
            //            dispZ = 0, // values eventually populated by element.Node.{displacements}
            //            rotXX = 0, // values eventually populated by element.Node.{displacements}
            //            rotYY = 0, // values eventually populated by element.Node.{displacements}
            //            rotZZ = 0, // values eventually populated by element.Node.{displacements}
            //            forceX = v3[i],
            //            forceY = v2[i],
            //            forceZ = p[i],
            //            momentXX = m3[i],
            //            momentYY = m2[i],
            //            momentZZ = t[i],
            //            axialStress = 0, // values eventually populated when element1d.section values are available
            //            shearStressY = 0, // values eventually populated when element1d.section values are available
            //            shearStressZ = 0, // values eventually populated when element1d.section values are available
            //            bendingStressYPos = 0, // values eventually populated when element1d.section values are available
            //            bendingStressYNeg = 0, // values eventually populated when element1d.section values are available
            //            bendingStressZPos = 0, // values eventually populated when element1d.section values are available
            //            bendingStressZNeg = 0, // values eventually populated when element1d.section values are available
            //            combinedStressMax = 0, // values eventually populated when element1d.section values are available
            //            combinedStressMin = 0 // values eventually populated when element1d.section values are available
            //        };

            //        results.Add(result);
            //    }
            //}

            return new ResultSet1D() { results1D = results };



        }

        public ResultSet1D SpandrelResultSet1dToSpeckle(string elementName)
        {
            List<Result1D> results = new List<Result1D>();

            SetLoadCombinationsForResults();

            // Reference variables for ETABS API
            int numberOfResults = 0;
            string[] storyName, spandrelName, loadCase, location;
            storyName = spandrelName = loadCase = location = new string[1];
            double[] p, v2, v3, t, m2, m3;
            p = v2 = v3 = t = m2 = m3 = new double[1];

            //Model.Results.SpandrelForce(ref numberOfResults, ref storyName, ref spandrelName, ref loadCase, ref location, ref p, ref v2, ref v3, ref t, ref m2, ref m3);

            //for (int i = 0; i < numberOfResults; i++)
            //{
            //    if (spandrelName[i] == elementName)
            //    {
            //        Result1D result = new Result1D()
            //        {
            //            element = new Element1D() { name = elementName }, // simple new Element1D until conversion class is created
            //            position = 0,
            //            description = "Location: " + location[i] + " - " + storyName[i],
            //            permutation = loadCase[i],
            //            dispX = 0, // values eventually populated by element.Node.{displacements}
            //            dispY = 0, // values eventually populated by element.Node.{displacements}
            //            dispZ = 0, // values eventually populated by element.Node.{displacements}
            //            rotXX = 0, // values eventually populated by element.Node.{displacements}
            //            rotYY = 0, // values eventually populated by element.Node.{displacements}
            //            rotZZ = 0, // values eventually populated by element.Node.{displacements}
            //            forceX = v3[i],
            //            forceY = v2[i],
            //            forceZ = p[i],
            //            momentXX = m3[i],
            //            momentYY = m2[i],
            //            momentZZ = t[i],
            //            axialStress = 0, // values eventually populated when element1d.section values are available
            //            shearStressY = 0, // values eventually populated when element1d.section values are available
            //            shearStressZ = 0, // values eventually populated when element1d.section values are available
            //            bendingStressYPos = 0, // values eventually populated when element1d.section values are available
            //            bendingStressYNeg = 0, // values eventually populated when element1d.section values are available
            //            bendingStressZPos = 0, // values eventually populated when element1d.section values are available
            //            bendingStressZNeg = 0, // values eventually populated when element1d.section values are available
            //            combinedStressMax = 0, // values eventually populated when element1d.section values are available
            //            combinedStressMin = 0 // values eventually populated when element1d.section values are available
            //        };

            //        results.Add(result);
            //    }
            //}

            return new ResultSet1D() { results1D = results };



        }

        public void SetLoadCombinationsForResults()
        {
            int numberOfLoadCombinations = 0;
            string[] loadCombinationNames = new string[1];

            Model.RespCombo.GetNameList(ref numberOfLoadCombinations, ref loadCombinationNames);

            foreach (var loadCombination in loadCombinationNames)
            {
                Model.Results.Setup.SetComboSelectedForOutput(loadCombination);
            }
        }
    }
}