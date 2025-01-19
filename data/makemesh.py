# *************************************************************
# This is the program to make 3D model for CFD by using Gmsh.
# input  : *.stl 
# output : *.msh, *.vtk
# *************************************************************

import gmsh
import math
import os
import sys
import numpy as np

gmsh.initialize(sys.argv)

# ===============================================
# input parameter
meshSize = 0.5 # mesh size
N = 5 # number of layers
r = 1.1 # ration
h = 0.05 # first_layer_thickness
# ===============================================

# ===============================================
# global variable
surface_real_wall = []
surface_fake_wall = []
surface_inlet_outlet = []
# ===============================================


# ===============================================
# Setting up the display of the gmsh GUI
def OptionSetting():
    # 1:ON / 0:OFF
    gmsh.option.setNumber("Mesh.VolumeEdges", 0)
    # ON/OFF of 2D mesh visualization 
    gmsh.option.setNumber("Mesh.SurfaceFaces", 1)
    # ?
    gmsh.option.setNumber("Mesh.Lines", 0)
    # Visualization of Entity in dimension 0 ON or OFF
    gmsh.option.setNumber("Geometry.PointLabels", 1)
    # Specify mesh line thickness
    gmsh.option.setNumber("Mesh.LineWidth", 4)
    # In gmsh, mouse wheel zoon on/zoom off is the opposite of paraview, so it should be with paraview
    gmsh.option.setNumber("General.MouseInvertZoom", 1)
    # Showing the rectangular memory surrounding the model so that the size of the model can be easily checked
    gmsh.option.setNumber("General.Axes", 3)
    # ?
    gmsh.option.setNumber("General.Trackball", 0)
    # Specifies the direction of the line of sight when the GUI is displayed as (0,0,0)
    gmsh.option.setNumber("General.RotationX", 0)
    gmsh.option.setNumber("General.RotationY", 0)
    gmsh.option.setNumber("General.RotationZ", 0)
    # Display information in gmsh terminal
    gmsh.option.setNumber("General.Terminal", 1)
# ===============================================

# ===============================================
# read stl
def ImportStl():
    # Absorb differences in directory and file paths between operating systems
    path = os.path.dirname(os.path.abspath(__file__))
    # read stl
    gmsh.merge(os.path.join(path, "WALL.stl"))

    # Decompose the loaded shape at a set angle
    # it takes time if forReparametrization is not True
    gmsh.model.mesh.classifySurfaces(angle = 40 * math.pi / 180, boundary=True, forReparametrization=True)
    # use with classifySurfaces
    gmsh.model.mesh.createGeometry()

    # gmsh.model.mesh.createTopology()

    # After the above steps, the shape of the surface was created.
    # In gmsh, we can retrieve the 2 dimensional objects among the surface-like shapes by
    # getEntities(2) to retrieve the two dimensional objects
    s_first = gmsh.model.getEntities(2)
    for i in range(len(s_first)):
        surface_real_wall.append(s_first[i][1])

    Syncronize()
# ===============================================

# ===============================================
# Create boundary layers and tetra meshes inside the boundary layers for regions
# Shape creation, such as setting volume
def ShapeCreation():
    gmsh.option.setNumber("Geometry.ExtrudeReturnLateralEntities", 0)

    # caution
    # The thickness of the boundary layer is not the thickness of each piece, but the distance from the reference line
    # So, t[i] += t[i - 1] and is summed below that layer
    n = np.linspace(1, 1, N) # [1, 1, 1, 1, 1]
    t = np.full(N, h) # distance from the reference line
    for i in range(0, N):
        t[i] = t[i] * r ** i
    for i in range(1, N):
        t[i] += t[i - 1]

    # Creating a boundary layer
    # Note the -t.
    # If -t is set, the boundary layer is stretched outside of the surface.
    e = gmsh.model.geo.extrudeBoundaryLayer(gmsh.model.getEntities(2), n, -t, True)

    # Get the surface of the last layer stacked among those stacked when the boundary layer was created
    top_ent = [s for s in e if s[0] == 2]
    for t in top_ent:
        surface_fake_wall.append(t[1])
    Syncronize()

    # Find the boundary (1D Entity) of top_ent (2D Entity)
    # Create the surface of the outflow inlet based on this
    bnd_ent = gmsh.model.getBoundary(top_ent)
    # An entity is represented as [(2, 1), (2, 5)].
    # Meaning.
    # The meaning is described in the form of the first Entity in two dimensions, 
    # so if you don't need the information of two dimensions, 
    # you can use the following form to extract only the information of the first Entity in two dimensions.
    # If you don't need the information about the second dimension, 
    # you can extract only the information about the number of the Entity as shown below.
    bnd_curv = [c[1] for c in bnd_ent]


    # Defines the contour of a closed surface inside the cross section of an inlet/outlet.
    closedSurfaceInletOutletInside = gmsh.model.geo.addCurveLoops(bnd_curv)
    print("closedSurfaceInletOutletInside = ", closedSurfaceInletOutletInside)
    for i in closedSurfaceInletOutletInside:
        # Stretch a closed surface on the contour created above
        eachClosedSurface = gmsh.model.geo.addPlaneSurface([i])

        surface_fake_wall.append(eachClosedSurface)
        surface_inlet_outlet.append(eachClosedSurface)

    # surface_fake_wall is a set of surfaces (2D Entity) surrounding the area where the tetra mesh is created
    innerSurfaceLoop = gmsh.model.geo.addSurfaceLoop(surface_fake_wall)
    # Assign the actual volume to the area created above
    gmsh.model.geo.addVolume([innerSurfaceLoop])

    Syncronize()
# ===============================================

# ===============================================

    # Basically, Don't change here.
    # To set boundary conditions in OpenFOAM,
    # Name the volumes on the faces and walls of the model.
    # Uppercase might be better. "SOMETHING", "WALL", ...
    # Entity candidate to correspond to stl in C# code is SOMETHING
def NamingBoundary():
    s_second = gmsh.model.getEntities(2)
    surfaceAll = []
    for i in range(len(s_second)):
        surfaceAll.append(s_second[i][1])
    surface_another = list(set(surfaceAll) - set(surface_real_wall) - set(surface_inlet_outlet))
    print(surface_another)
    # python set symmetry (physics)
    something = list(set(surface_another) ^ set(surface_fake_wall))
    print(something)
    gmsh.model.addPhysicalGroup(2, something, 99)
    gmsh.model.setPhysicalName(2, 99, "SOMETHING")

    # INLET = [320, 57]
    # OUTLET = [321, 74]
    # gmsh.model.addPhysicalGroup(2, INLET, 11)
    # gmsh.model.setPhysicalName(2, 11, "INLET")
    # gmsh.model.addPhysicalGroup(2, OUTLET, 12)
    # gmsh.model.setPhysicalName(2, 12, "OUTLET")

    wall = surface_real_wall
    gmsh.model.addPhysicalGroup(2, wall, 10)
    gmsh.model.setPhysicalName(2, 10, "WALL")

    volumeAll = gmsh.model.getEntities(3)
    three_dimension_list = []
    for i in range(len(volumeAll)):
        three_dimension_list.append(volumeAll[i][1])
    gmsh.model.addPhysicalGroup(3, three_dimension_list, 100)
    gmsh.model.setPhysicalName(3, 100, "INTERNAL")

    # INLET_LIST = [13, 16]
    # gmsh.model.addPhysicalGroup(2, INLET_LIST, 20)
    # gmsh.model.setPhysicalName(2, 20, "INLET")

    # OUTLET_LIST = [9, 15]
    # gmsh.model.addPhysicalGroup(2, OUTLET_LIST, 30)
    # gmsh.model.setPhysicalName(2, 30, "OUTLET")

    Syncronize()
# ===============================================

# ===============================================
# To check the completed mesh with GUI
# To visualize a large mesh on a low performance PC, it will be heavy or crash in the worst case.
def ConfirmMesh():
    if "-nopopup" not in sys.argv:
        gmsh.fltk.run()
# ===============================================

# ===============================================
# file output
# write("~~~~.extension")
# .msh files are used for OpenFOAM, which outputs a mesh for the extension depending on the extension.
# To use .msh for OpenFOAM, the file format version of .msh must be 2.2.
# vtk files are also output for viewing in paraview.
def OutputMshVtk():
    gmsh.option.setNumber("Mesh.MshFileVersion", 2.2)
    gmsh.write("MeshOriginal.msh")
    gmsh.write("MeshOriginal.vtk")
# ===============================================

# ===============================================
# make mesh
def Meshing():
    gmsh.option.setNumber("Mesh.OptimizeNetgen", 1)
    # Set threshold for mesh optimization
    # 0 is no optimization
    # 1 is maximum optimization
    gmsh.option.setNumber("Mesh.OptimizeThreshold", 0.9)
    # Set mesh algorithm
    gmsh.option.setNumber('Mesh.Algorithm', 1)
    # How many times to repeat optimization->why the quality gets worse
    # gmsh.option.setNumber(“Mesh.Optimize”, 10)
    # Overall mesh control
    # interpolate between Min and Max
    # Basically, it is cut based on Max
    # Min may be meaningless
    gmsh.option.setNumber("Mesh.MeshSizeMin", meshSize)
    gmsh.option.setNumber("Mesh.MeshSizeMax", meshSize)
    gmsh.model.mesh.generate(3)
    gmsh.model.mesh.optimize()
    print("finish meshing")
# ===============================================

# ===============================================
# Defined shapes, etc. in gmsh shape kernel? Reflected in
# Functionalized as it is used many times
def Syncronize():
    gmsh.model.geo.synchronize()
# ===============================================

OptionSetting()
ImportStl()
ShapeCreation()
NamingBoundary()
Meshing()
OutputMshVtk()
ConfirmMesh()

gmsh.finalize()