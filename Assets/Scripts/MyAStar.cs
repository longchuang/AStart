﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MyAStar : MonoBehaviour
{
	/// <summary>
	/// 单例脚本
	/// </summary>
	public static MyAStar instance;

	// 白色格子
	public GameObject reference;
	//格子数组
	public Grid[,] grids;
	//格子数组对应的参考物（方块）对象
	public GameObject[,] objs;
	// 开启和关闭列表
	public List<Grid> openList;
	public List<Grid> closeList;
	//目标点坐标
	public int targetX;
	public int targetY;
	//起始点坐标
	public int startX;
	public int startY;

	//格子行列数
	private int row;
	private int colomn;
	//结果栈
	private Stack<string> parentList;
	//基础物体
	private Transform plane;
	//流颜色参数
	private float alpha = 0;
	private float incrementPer = 0;

	void Awake ()
	{
		instance = this;
		plane = GameObject.Find ("Plane").transform;
		parentList = new Stack<string> ();
		openList = new List<Grid> ();
		closeList = new List<Grid> ();
	}

	void Start ()
	{
		Init ();
		StartCoroutine (Count ());

	}


	/// <summary>
	/// 初始化操作
	/// </summary>
	void Init ()
	{
		//计算行列数
		// 单位1，放大20倍，每行放20个格子
		int x = (int)(plane.localScale.x * 20);
		int y = (int)(plane.localScale.z * 20);
		row = x;
		colomn = y;
		// 创建20*20的格子
		grids = new Grid[x, y];
		objs = new GameObject[x, y];
		// 白色方块起始坐标（-2，0，-2）
		Vector3 startPos = 
			new Vector3 (plane.localScale.x*-2 , 0, plane.localScale.z*-2 );
		//生成参考物体（Cube）
		for (int i = 0; i < x; i++) {
			for (int j = 0; j < y; j++) {
				grids [i, j] = new Grid (i, j);
				GameObject item = (GameObject)Instantiate (reference, 
					                  new Vector3 (i * 0.5f, 0, j * 0.5f) + startPos, 
					                  Quaternion.identity);
				
				item.GetComponentInChildren<Reference> ().x = i;
				item.GetComponentInChildren<Reference> ().y = j;
				objs [i, j] = item;
			}
		}
	}

	/// <summary>
	/// A*计算
	/// </summary>
	IEnumerator Count ()
	{
		//等待前面操作完成
		yield return new WaitForSeconds (0.1f);
		//添加起始点
		openList.Add (grids [startX, startY]);
		//声明当前格子变量，并赋初值
		Grid currentGrid = openList [0];
		//循环遍历路径最小F的点
		while (openList.Count > 0 && currentGrid.type != GridType.End) {
			//获取此时最小F点,因为移除后[0]表示的不是同一个格子
			currentGrid = openList [0];
			//如果当前点就是目标
			if (currentGrid.type == GridType.End) {
				Debug.Log ("Find");

				//生成结果
				GenerateResult (currentGrid);

				StartCoroutine (ShowResult ());
			}
			//上下左右，左上左下，右上右下，遍历
			for (int i = -1; i <= 1; i++) {
				for (int j = -1; j <= 1; j++) {
					// no self
					if (i != 0 || j != 0) {
						//计算坐标
						int x = currentGrid.x + i;
						int y = currentGrid.y + j;
						//如果未超出所有格子范围，不是障碍物，不是重复点
						if (x >= 0 && y >= 0 && x < row && y < colomn
						    && grids [x, y].type != GridType.Obstacle
						    && !closeList.Contains (grids [x, y])) {
							// 计算G值
							int g = currentGrid.g + (int)(Mathf.Sqrt (i*i+j*j) * 10);
							//与原G值对照
							if (grids [x, y].g == 0 || grids [x, y].g > g) {
								//更新G值
								grids [x, y].g = g;
								// 当前九个格子的父物体都是，中间的格子
								grids [x, y].parent = currentGrid;
							}
							//计算H值
							grids [x, y].h = Manhattan (x, y);
							//计算F值
							grids [x, y].f = grids [x, y].g + grids [x, y].h;
							//如果未添加到开启列表
							if (!openList.Contains (grids [x, y])) {
								//添加
								openList.Add (grids [x, y]);
							}
							//重新排序,把f值小的，放到数组前面,路径选择需要选择F值最小的
							openList.Sort ();
//							foreach (var item in openList) {
//								Debug.Log (item.f);
//							}
						}
					}
				}
			}
			//完成遍历添加该点到关闭列表
			closeList.Add (currentGrid);
			//从开启列表中移除
			openList.Remove (currentGrid);
			//如果开启列表空，未能找到路径
			if (openList.Count == 0) {
				Debug.Log ("Can not Find");
			}
		}


	}

	/// <summary>
	/// 生成结果
	/// </summary>
	/// <param name="currentGrid">Current grid.</param>
	/// 找到终点之后，才会执行递归，传进去的是最后一个格子
	/// 因为先进后出的原则，所以整个进栈的过程是从结尾->开始
	/// 所以读取的时候就是从开始->结尾
	void GenerateResult (Grid currentGrid)
	{
		//如果当前格子有父格子
		if (currentGrid.parent != null) {
			//添加到父对象栈（即结果栈）
			parentList.Push (currentGrid.x + "|" + currentGrid.y);
			//递归获取
			GenerateResult (currentGrid.parent);
		}
	}

	/// <summary>
	/// 显示结果
	/// </summary>
	/// <returns>The result.</returns>
	IEnumerator ShowResult ()
	{
		//等待前面计算完成
		yield return null;
		//计算每帧颜色值增量
		incrementPer = 1 / (float)parentList.Count;
		//展示结果
		while (parentList.Count != 0) {
			//出栈
			/// 因为先进后出的原则，所以整个进栈的过程是从结尾->开始
			/// 所以读取的时候就是从开始->结尾
			string str = parentList.Pop ();
			//等0.3秒
			yield return new WaitForSeconds (0.3f);
			//拆分获取坐标,分割，得到数组
			string[] xy = str.Split (new char[]{ '|' });
			int x = int.Parse (xy [0]);
			int y = int.Parse (xy [1]);
			//当前颜色值
			alpha += incrementPer;
			//以颜色方式绘制路径
			objs [x, y].GetComponentInChildren<MeshRenderer> ().material.color
			= new Color (1 - alpha, alpha, 0, 1);
		}

		StopAllCoroutines ();
	}

	/// <summary>
	/// 曼哈顿方式计算H值
	/// </summary>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	int Manhattan (int x, int y)
	{
		return (int)(Mathf.Abs (targetX - x) + Mathf.Abs (targetY - y)) * 10;
	}


}
