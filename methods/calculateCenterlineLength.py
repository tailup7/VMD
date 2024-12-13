# 点群で表現される曲線の、各点間の距離と全体の長さ(各点間の距離の総和)を出力する
# 入力ファイルは2行目以降に 各行に曲線上の順番通りに点の x, y, z 座標が 書かれているもの 

import os
import math
import tkinter as tk
from tkinter import filedialog
from itertools import zip_longest

def read_points_from_file(input_filename):
    points = []
    with open(input_filename, 'r') as file:
        next(file)  # 1行目は点の総数を書いているのでスキップ
        for line in file:
            x, y, z = map(float, line.split())
            points.append((x, y, z))
    return points

def calculate_distance(p1, p2):
    return math.sqrt((p2[0] - p1[0])**2 + (p2[1] - p1[1])**2 + (p2[2] - p1[2])**2)

def calculate_adjacent_distances(points):
    distances = []
    for i in range(len(points) - 1):
        dist = calculate_distance(points[i], points[i + 1])
        distances.append(dist)
    return distances

def write_to_textfile(distances1, distances2, input_filename1, input_filename2):
    output_filename = f"calculateLength_{os.path.splitext(os.path.basename(input_filename1))[0]}_" \
                    f"{os.path.splitext(os.path.basename(input_filename2))[0]}.txt"
    with open(output_filename, 'w') as file:
        file.write("count of point to point\n")
        file.write(f"{len(distances1)}\t{len(distances2)}\n")
        file.write("length of point to point\n")
        for dist1, dist2 in zip_longest (distances1, distances2, fillvalue=""):
            if dist1 != "":
                file.write(f"{dist1:.4f}\t")
            else:
                file.write("\t")
            
            if dist2 != "":
                file.write(f"{dist2:.4f}\n")
            else:
                file.write("\n")
        file.write(f"Total length \n ") 
        file.write(f"{sum(distances1):.4f}\t{sum(distances2):.4f}\n")

def write_to_textfile_sub(distances1, input_filename1):
    output_filename = f"calculateLength_{os.path.splitext(os.path.basename(input_filename1))[0]}.txt" 
    with open(output_filename, 'w') as file:
        file.write("count of point to point\n")
        file.write(f"{len(distances1)}\n")
        file.write("length of point to point\n")
        for dist1 in (distances1):
            file.write(f"{dist1:.4f}\n")
        file.write(f"Total length \n")
        file.write(f"{sum(distances1):.4f}\n")

def choose_file():
    root = tk.Tk()
    root.withdraw()  
    filename = filedialog.askopenfilename(title="Select file", filetypes=[("Text Files", "*.txt")])
    return filename

def sub(input_filename1):
    points1 = read_points_from_file(input_filename1)
    distances1 = calculate_adjacent_distances(points1)

    write_to_textfile_sub(distances1, input_filename1)

    for dist1 in (distances1):
        print(f"{dist1:.4f}")
    print(f"Total length")
    print(f"{sum(distances1):.4f}\n")

def main():
    input_filename1 = choose_file()
    if not input_filename1:
        print("No file selected, exiting.")
        return 
    
    input_filename2 = choose_file()
    if not input_filename2:
        sub(input_filename1)
        return
    
    points1 = read_points_from_file(input_filename1)  
    points2 = read_points_from_file(input_filename2)
    distances1 = calculate_adjacent_distances(points1) 
    distances2 = calculate_adjacent_distances(points2)
    
    write_to_textfile(distances1, distances2, input_filename1, input_filename2)

    for dist1,dist2 in zip_longest(distances1, distances2, fillvalue=""):
        if dist1 != "":
            print(f"{dist1:.4f}", end="\t")
        else:
            print("\t", end="")
        
        if dist2 != "":
            print(f"{dist2:.4f}")
        else:
            print()
    print(f"Total length")
    print(f"{sum(distances1):.4f}\t{sum(distances2):.4f}")

if __name__ == '__main__':
    main()