import gmsh
import math
import os
import sys
import numpy as np

gmsh.initialize(sys.argv)

stlPath = sys.argv[1]
outputMeshPath = sys.argv[2]
outputVTKPath = sys.argv[3]

# ===============================================
# パラメータ
# 書き換えるのはここだけ
meshSize = 0.3 # mesh size
# N = 5 # number of layers
# r = 1.1 # ration
# h = 0.05 # first_layer_thickness
# ===========================



# ===============================================
# グローバル変数
surface_real_wall = []
surface_fake_wall = []
surface_inlet_outlet = []
# ===============================================



# ===============================================
# gmshのGUIの表示の設定
def OptionSetting():
    # 1がON/0がOFF
    # 2次元メッシュの可視化ON/OFF
    gmsh.option.setNumber("Mesh.SurfaceFaces", 1)
    # ?
    gmsh.option.setNumber("Mesh.Lines", 1)
    # 0次元のEntityの可視化ON?OFF
    gmsh.option.setNumber("Geometry.PointLabels", 1)
    # メッシュの線の太さを指定
    gmsh.option.setNumber("Mesh.LineWidth", 4)
    # gmshではマウスのホイールのズーンオン/ズームオフがparaviewとは逆なので、paraviewと一緒にする
    gmsh.option.setNumber("General.MouseInvertZoom", 1)
    # モデルのサイズが簡単に確認できるように、モデルを囲む直方体メモリを表示
    gmsh.option.setNumber("General.Axes", 3)
    # ?
    gmsh.option.setNumber("General.Trackball", 0)
    # GUIが表示された時の、目線の方向を(0,0,0)に指定
    gmsh.option.setNumber("General.RotationX", 0)
    gmsh.option.setNumber("General.RotationY", 0)
    gmsh.option.setNumber("General.RotationZ", 0)
    # gmshのターミナルに情報を表示
    gmsh.option.setNumber("General.Terminal", 1)
# ===============================================


# ===============================================
# stlの読み込み
def ImportStl():
    # ディレクトリやファイルのパスのOSごとの差を吸収
    path = os.path.dirname(os.path.abspath(__file__))
    # stlの読み込み
    gmsh.merge(os.path.join(path, stlPath))

    # 読み込んだ形状を設定した角度で分解
    # forReparametrizationをTrueにしないとメッシングで時間がかかる
    # gmsh.model.mesh.classifySurfaces(angle = 40 * math.pi / 180, boundary=True, forReparametrization=True)
    # classifySurfacesとセットで用いる
    # gmsh.model.mesh.createGeometry()

    # メッシュのトポロジー（頂点、エッジ、面、体積の関係）を作成する関数
    # STLファイルで定義された形状をGMSHが理解し、メッシュの生成を行う準備をする
    gmsh.model.mesh.createTopology()

    # STLモデル内のすべての2次元の面のエンティティを取得
    s_first = gmsh.model.getEntities(2)
    for i in range(len(s_first)):
        surface_real_wall.append(s_first[i][1])
    # GMSHモデルに追加したジオメトリ情報（STLファイルから読み込んだ情報）と、それに基づいたメッシュ情報を同期
    Syncronize()
# ===============================================



# ===============================================
# 境界層の作成や、境界層より内側のテトラメッシュを領域に対して
# Volumeを設定するなどの形状作成
def ShapeCreation():
    # gmsh.option.setNumber("Geometry.ExtrudeReturnLateralEntities", 0)

    # # 注意
    # # 境界層の厚さが一枚ごとの厚さではなく、基準線からの距離
    # # なので、t[i] += t[i - 1]で、その層以下の総和をしている
    # n = np.linspace(1, 1, N) # [1, 1, 1, 1, 1]
    # t = np.full(N, h) # distance from the reference line
    # for i in range(0, N):
    #     t[i] = t[i] * r ** i
    # for i in range(1, N):
    #     t[i] += t[i - 1]



    # # 境界層の作成
    # # -tに注意
    # # tにすると表面の外側に境界層が張られる
    # e = gmsh.model.geo.extrudeBoundaryLayer(gmsh.model.getEntities(2), n, -t, True)


    # 境界層を作成したときに積層された中で最も最後に積層された層の表面を取得
    top_ent = gmsh.model.getEntities(2)
    # top_ent = [s for s in e if s[0] == 2]
    for t in top_ent:
        surface_fake_wall.append(t[1])
    Syncronize()


    # top_ent(2次元のEntity)の境界(1次元のEntity)を求める
    # これをもとにして流出入部の面を作成する
    bnd_ent = gmsh.model.getBoundary(top_ent)
    # entityは[(2, 1), (2, 5)]というような形で表現される
    # 意味は
    # 2次元の中の一番目のEntity、という形で記述されるので、2次元という情報がいらない場合は
    # 下のような形で、何番目かという情報だけ抜き出せる
    bnd_curv = [c[1] for c in bnd_ent]


    # 流出入部の断面の内側の、閉曲面の輪郭を定義
    closedSurfaceInletOutletInside = gmsh.model.geo.addCurveLoops(bnd_curv)
    print("closedSurfaceInletOutletInside = ", closedSurfaceInletOutletInside)
    for i in closedSurfaceInletOutletInside:
        # 上記で作成された輪郭に閉曲面を張る
        eachClosedSurface = gmsh.model.geo.addPlaneSurface([i])

        surface_fake_wall.append(eachClosedSurface)
        surface_inlet_outlet.append(eachClosedSurface)


    # surface_fake_wallはテトラメッシュを作成する領域を囲む表面(2次元Entity)の集合
    innerSurfaceLoop = gmsh.model.geo.addSurfaceLoop(surface_fake_wall)
    # 上記で作った領域に、実際の体積を割り当て
    gmsh.model.geo.addVolume([innerSurfaceLoop])

    Syncronize()
# ===============================================


# ===============================================

    # 基本的にここの変更はしない

# OpenFOAMで境界条件を設定するために、
# モデルの面や壁に体積に名前をつける
# 基本大文字が良いかも
# C#コードでstlと対応させるEntityの候補はSOMETHINGにしている
def NamingBoundary():
    s_second = gmsh.model.getEntities(2)
    surfaceAll = []
    for i in range(len(s_second)):
        surfaceAll.append(s_second[i][1])
    surface_another = list(set(surfaceAll) - set(surface_real_wall) - set(surface_inlet_outlet))
    print(surface_another)
    # python set 対称差
    something = list(set(surface_another) ^ set(surface_fake_wall))
    print(f"something = {something}")
    # remove 1
    something = [s for s in something if s != 1]
    print(f"newSomething = {something}")
    # something = [2, 3]
    gmsh.model.addPhysicalGroup(2, something, 99)
    gmsh.model.setPhysicalName(2, 99, "SOMETHING")

    # something = [2, 3]
    # inlet = [2]
    # outlet = [3]
    # # gmsh.model.addPhysicalGroup(2, something, 99)
    # # gmsh.model.setPhysicalName(2, 99, "SOMETHING")
    # gmsh.model.addPhysicalGroup(2, inlet, 11)
    # gmsh.model.setPhysicalName(2, 11, "INLET")
    # gmsh.model.addPhysicalGroup(2, outlet, 12)
    # gmsh.model.setPhysicalName(2, 12, "OUTLET")

    wall = surface_real_wall
    gmsh.model.addPhysicalGroup(2, wall, 90)
    gmsh.model.setPhysicalName(2, 90, "INNERWALL")

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
# 完成したメッシュをGUIで確認するため
def ConfirmMesh():
    if "-nopopup" not in sys.argv:
        gmsh.fltk.run()
# ===============================================


# ===============================================
# 定義した形状などをgmshの形状カーネル？に反映
# 何度も使うので関数化
def Syncronize():
    gmsh.model.geo.synchronize()
# ===============================================


# ===============================================
# ファイル出力
# write("~~~~.拡張子")
# 拡張子によって、その拡張子用にメッシュが出力される
# OpenFOAMに用いるのは.mshファイル
# .mshをOpneFOAMで用いるには、.mshのファイルフォーマットバージョンを2.2にする必要がある
# paraviewで見るように.vtkファイルも出力
def OutputMshVtk():
    gmsh.option.setNumber("Mesh.MshFileVersion", 2.2)
    gmsh.write(outputMeshPath)
    gmsh.write(outputVTKPath)
# ===============================================


# ===============================================
# メッシュ作成
def Meshing():
    gmsh.option.setNumber("Mesh.OptimizeNetgen", 1)
    # メッシュ最適化の閾値を設定
    # 0が最適化なし
    # 1が最適化最大
    gmsh.option.setNumber("Mesh.OptimizeThreshold", 0.9)
    # メッシュのアルゴリズムを設定
    gmsh.option.setNumber('Mesh.Algorithm', 1)
    # 最適化を何回繰り返すか->なぜか品質わるくなる
    # gmsh.option.setNumber("Mesh.Optimize", 10)
    # 全体的なメッシュの制御
    # MinとMaxではさむ
    # 基本的にMaxを基準に切られる
    # Minは意味ないかも
    gmsh.option.setNumber("Mesh.MeshSizeMin", meshSize)
    gmsh.option.setNumber("Mesh.MeshSizeMax", meshSize)
    gmsh.model.mesh.generate(3)
    gmsh.model.mesh.optimize()
    print("finish meshing")
# ===============================================


OptionSetting()
ImportStl()
ShapeCreation()
NamingBoundary()
Meshing()
OutputMshVtk()
# ConfirmMesh()

gmsh.finalize()
