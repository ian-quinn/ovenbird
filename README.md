# Coenobita :crab:

![.NET](https://img.shields.io/badge/.NET-4.8-green.svg)  

Personal algorithm test modules.

```
Coenobita
├ Coenobita.gha         - compiled addin file
├ test.gh               - component test file
├ /Scripts              - legacy code   
│ ├ *.gh                - gh file for testing
│ └ *.cs                - raw code by ScriptsParasite
└ /Coenobita
  ├ /Properties         - info
  ├ /Resources          - icons
  ├ /Geometry           - testing algorithms
  ├ Coenobita.sln       - VS solution file
  └ Module$.cs          - component entrance
```

`ModuleGetHull` Plans: 1. Rectangular box for a set of 2D points; 2. Minimum orthogonal hull for a set of 2D points; 3. Minimum concave hull for a set of 2D segments.

`ModuleTesselation` Plans: 1. Rectangular tessellation for rectangular polygon [@](https://discourse.mcneel.com/t/divide-surface-based-on-corners/84805) (only tolerating few oblique edges); 2. Hertel Mehlhorn Convex Decomposition. 

`ModuleUnzipXML` Plans: 1. A fast gbXML loop geometry navigator; 2. Space topology navigator (a graph generator).