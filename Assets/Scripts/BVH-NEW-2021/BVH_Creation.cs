using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BVH_Creation : MonoBehaviour
{
    public int NumberOfMaxChildren = 4;


    public struct BVHElement
    {
        public BVHElement(GameObject gameObject, int i)
        {
            //isLeaf = leaf;
            //isRoot = root;
            //go = gameObject;
            bb = new Bounds(gameObject.transform.position, Vector3.Scale(gameObject.transform.localScale, gameObject.GetComponent<MeshFilter>().mesh.bounds.size));

            //childrenIndexs = new List<int>();
            meshID = i;
            //bb = gameObject.GetComponent<MeshFilter>().mesh.bounds;
        }

        //public bool isLeaf;
        //public bool isRoot;
        public Bounds bb;
        //public GameObject go;

        //public List<int> childrenIndexs;

        public int meshID;
    }

    List<BVHElement> BvhElementInScene;

    public List<BoundingVolumeHierarchy> BVHInScene;


    public struct BoundingVolumeHierarchy
    {
        public bool leaf;
        public bool root;
        public bool empty;
        public Bounds boundingBox;
        public List<int> childrenIndex;
        public int fatherIndex;

        public int meshIndex;
    }

    private int indx;

    public static int SortByName(GameObject o1, GameObject o2)
    {
        return o1.name.CompareTo(o2.name);
    }

    // Start is called before the first frame update
    void Start()
    {
        List<GameObject> bvhBleObjsInScene = new List<GameObject>(GameObject.FindGameObjectsWithTag("BVHble"));

        bvhBleObjsInScene.Sort(SortByName);

        BvhElementInScene = new List<BVHElement>();

        for (int i=0; i<bvhBleObjsInScene.Count; i++)
        {
            //Debug.Log(bvhBleObjsInScene[i].name);
            BvhElementInScene.Add(new BVHElement(bvhBleObjsInScene[i],i));
        }

        //foreach (BVHElement b in BvhElementInScene)
            //Debug.Log(b.bb.size);

        BVHInScene = new List<BoundingVolumeHierarchy>();
        BVHInScene.Add(CreateRootNode(BvhElementInScene));

        indx = 0;

        CreateBoundingVolumeHierarchy(BvhElementInScene);


        Debug.Log("OBJECTS = " + BvhElementInScene.Count);
        Debug.Log("BVHs = " + BVHInScene.Count);

        foreach (BoundingVolumeHierarchy bv in BVHInScene)
        {
            if (bv.leaf)
                Debug.Log(bvhBleObjsInScene[bv.meshIndex]);
            //if (bv.childrenIndex != null)
            //    Debug.Log(bv.childrenIndex.Count);
        }


    }



    //all this can be maybe done in one for with 8 bb   (switch case on i <8 1, 2, 3, 4, 5, 6, 7, 8)
    private void CreateBoundingVolumeHierarchy(List<BVHElement> workingList)
    {
        //creare una bvh si può fare in diversi modi:

        int rootIndex = indx;

        //8 figli per ogni padre

        BoundingVolumeHierarchy son1 = new BoundingVolumeHierarchy();
        son1.fatherIndex = rootIndex;
        son1.root = false;
        Vector3 centerBB = new Vector3((BVHInScene[rootIndex].boundingBox.center.x + BVHInScene[rootIndex].boundingBox.min.x) / 2, (BVHInScene[rootIndex].boundingBox.center.y + BVHInScene[rootIndex].boundingBox.min.y) / 2, (BVHInScene[rootIndex].boundingBox.center.z + BVHInScene[rootIndex].boundingBox.min.z) / 2);
        son1.boundingBox = new Bounds(centerBB, BVHInScene[rootIndex].boundingBox.extents + new Vector3(0.001f, 0.001f, 0.001f));

        //check if son1 contains anything
        List<BVHElement> workingListSon = new List<BVHElement>();


        for (int i= 0; i< workingList.Count; i++)
        {
            if (son1.boundingBox.Contains(workingList[i].bb.center))
            {
                //it contains something
                workingListSon.Add(workingList[i]);
                workingList.RemoveAt(i);
            }
        }

        if (workingListSon.Count > 0)   //if yes -> use this son
        {
            indx++;
            BVHInScene[rootIndex].childrenIndex.Add(indx);
            if (workingListSon.Count < NumberOfMaxChildren + 1)   //if it contains 1 element -> this is a leaf
            {
                if (NumberOfMaxChildren == 1)
                {
                    son1.leaf = true;
                    son1.meshIndex = workingListSon[0].meshID;
                    son1.boundingBox = workingListSon[0].bb;
                }
                else
                {
                    int sonIndex = indx;
                    son1.leaf = false;
                    son1.empty = false;
                    son1.childrenIndex = new List<int>();
                    //change BB in order to fit all objects

                    son1.boundingBox = workingListSon[0].bb;
                    foreach (BVHElement b in workingListSon)
                    {
                        son1.boundingBox.Encapsulate(b.bb);
                        indx++;
                        son1.childrenIndex.Add(indx);
                        BoundingVolumeHierarchy leafCh = new BoundingVolumeHierarchy
                        {
                            fatherIndex = sonIndex,
                            leaf = true,
                            empty = false,
                            root = false,
                            boundingBox = b.bb,
                            meshIndex = b.meshID,
                        };
                        BVHInScene.Add(leafCh);
                    }
                }
                BVHInScene.Add(son1);
            }
            else    //if it contains >1 elements -> go deeper
            {
                son1.leaf = false;
                son1.empty = false;
                son1.childrenIndex = new List<int>();
                //change BB in order to fit all objects

                son1.boundingBox = workingListSon[0].bb;
                foreach(BVHElement b in workingListSon)
                {
                    son1.boundingBox.Encapsulate(b.bb);
                }
                son1.boundingBox.size += new Vector3(0.001f, 0.001f, 0.001f);
                BVHInScene.Add(son1);
                CreateBoundingVolumeHierarchy(workingListSon);
            }
        }
        else    //if not -> not use this son
        {
            son1.empty = true;
        }

        

        BoundingVolumeHierarchy son2 = new BoundingVolumeHierarchy();
        son2.fatherIndex = rootIndex;
        son2.root = false;
        centerBB = new Vector3((BVHInScene[rootIndex].boundingBox.center.x + BVHInScene[rootIndex].boundingBox.min.x) / 2, (BVHInScene[rootIndex].boundingBox.center.y + BVHInScene[rootIndex].boundingBox.max.y) / 2, (BVHInScene[rootIndex].boundingBox.center.z + BVHInScene[rootIndex].boundingBox.min.z) / 2);
        son2.boundingBox = new Bounds(centerBB, BVHInScene[rootIndex].boundingBox.extents + new Vector3(0.001f, 0.001f, 0.001f));

        //check if son1 contains anything
        workingListSon = new List<BVHElement>();
        for (int i = 0; i < workingList.Count; i++)
        {
            if (son2.boundingBox.Contains(workingList[i].bb.center))
            {
                //it contains something
                workingListSon.Add(workingList[i]);
                workingList.RemoveAt(i);
            }
        }

        if (workingListSon.Count > 0)   //if yes -> use this son
        {
            indx++;
            BVHInScene[rootIndex].childrenIndex.Add(indx);
            if (workingListSon.Count < NumberOfMaxChildren + 1)   //if it contains 1 element -> this is a leaf
            {
                if (NumberOfMaxChildren == 1)
                {
                    son2.leaf = true;
                    son2.meshIndex = workingListSon[0].meshID;
                    son2.boundingBox = workingListSon[0].bb;
                }
                else
                {
                    int sonIndex = indx;
                    son2.leaf = false;
                    son2.empty = false;
                    son2.childrenIndex = new List<int>();
                    //change BB in order to fit all objects

                    son2.boundingBox = workingListSon[0].bb;
                    foreach (BVHElement b in workingListSon)
                    {
                        son2.boundingBox.Encapsulate(b.bb);
                        indx++;
                        son2.childrenIndex.Add(indx);
                        BoundingVolumeHierarchy leafCh = new BoundingVolumeHierarchy
                        {
                            fatherIndex = sonIndex,
                            leaf = true,
                            empty = false,
                            root = false,
                            boundingBox = b.bb,
                            meshIndex = b.meshID,
                        };
                        BVHInScene.Add(leafCh);
                    }
                }
                BVHInScene.Add(son2);
            }
            else    //if it contains >1 elements -> go deeper
            {
                son2.leaf = false;
                son2.empty = false;
                son2.childrenIndex = new List<int>();
                //change BB in order to fit all objects

                son2.boundingBox = workingListSon[0].bb;
                foreach (BVHElement b in workingListSon)
                {
                    son2.boundingBox.Encapsulate(b.bb);
                }
                son2.boundingBox.size += new Vector3(0.001f, 0.001f, 0.001f);

                BVHInScene.Add(son2);
                CreateBoundingVolumeHierarchy(workingListSon);
            }
        }
        else    //if not -> not use this son
        {
            son2.empty = true;
        }

        BoundingVolumeHierarchy son3 = new BoundingVolumeHierarchy();
        son3.fatherIndex = rootIndex;
        son3.root = false;
        centerBB = new Vector3((BVHInScene[rootIndex].boundingBox.center.x + BVHInScene[rootIndex].boundingBox.min.x) / 2, (BVHInScene[rootIndex].boundingBox.center.y + BVHInScene[rootIndex].boundingBox.min.y) / 2, (BVHInScene[rootIndex].boundingBox.center.z + BVHInScene[rootIndex].boundingBox.max.z) / 2);
        son3.boundingBox = new Bounds(centerBB, BVHInScene[rootIndex].boundingBox.extents + new Vector3(0.001f, 0.001f, 0.001f));

        //check if son1 contains anything
        workingListSon = new List<BVHElement>();
        for (int i = 0; i < workingList.Count; i++)
        {
            if (son3.boundingBox.Contains(workingList[i].bb.center))
            {
                //it contains something
                workingListSon.Add(workingList[i]);
                workingList.RemoveAt(i);
            }
        }

        if (workingListSon.Count > 0)   //if yes -> use this son
        {
            indx++;
            BVHInScene[rootIndex].childrenIndex.Add(indx);
            if (workingListSon.Count < NumberOfMaxChildren + 1)   //if it contains 1 element -> this is a leaf
            {
                if (NumberOfMaxChildren == 1)
                {
                    son3.leaf = true;
                    son3.meshIndex = workingListSon[0].meshID;
                    son3.boundingBox = workingListSon[0].bb;
                }
                else
                {
                    int sonIndex = indx;
                    son3.leaf = false;
                    son3.empty = false;
                    son3.childrenIndex = new List<int>();
                    //change BB in order to fit all objects

                    son3.boundingBox = workingListSon[0].bb;
                    foreach (BVHElement b in workingListSon)
                    {
                        son3.boundingBox.Encapsulate(b.bb);
                        indx++;
                        son3.childrenIndex.Add(indx);
                        BoundingVolumeHierarchy leafCh = new BoundingVolumeHierarchy
                        {
                            fatherIndex = sonIndex,
                            leaf = true,
                            empty = false,
                            root = false,
                            boundingBox = b.bb,
                            meshIndex = b.meshID,
                        };
                        BVHInScene.Add(leafCh);
                    }
                }
                BVHInScene.Add(son3);
            }
            else    //if it contains >1 elements -> go deeper
            {
                son3.leaf = false;
                son3.empty = false;
                son3.childrenIndex = new List<int>();
                //change BB in order to fit all objects

                son3.boundingBox = workingListSon[0].bb;
                foreach (BVHElement b in workingListSon)
                {
                    son3.boundingBox.Encapsulate(b.bb);
                }
                son3.boundingBox.size += new Vector3(0.001f, 0.001f, 0.001f);

                BVHInScene.Add(son3);
                CreateBoundingVolumeHierarchy(workingListSon);
            }
        }
        else    //if not -> not use this son
        {
            son3.empty = true;
        }

        BoundingVolumeHierarchy son4 = new BoundingVolumeHierarchy();
        son4.fatherIndex = rootIndex;
        son4.root = false;
        centerBB = new Vector3((BVHInScene[rootIndex].boundingBox.center.x + BVHInScene[rootIndex].boundingBox.min.x) / 2, (BVHInScene[rootIndex].boundingBox.center.y + BVHInScene[rootIndex].boundingBox.max.y) / 2, (BVHInScene[rootIndex].boundingBox.center.z + BVHInScene[rootIndex].boundingBox.max.z) / 2);
        son4.boundingBox = new Bounds(centerBB, BVHInScene[rootIndex].boundingBox.extents + new Vector3(0.001f, 0.001f, 0.001f));

        //check if son1 contains anything
        workingListSon = new List<BVHElement>();
        for (int i = 0; i < workingList.Count; i++)
        {
            if (son4.boundingBox.Contains(workingList[i].bb.center))
            {
                //it contains something
                workingListSon.Add(workingList[i]);
                workingList.RemoveAt(i);
            }
        }

        if (workingListSon.Count > 0)   //if yes -> use this son
        {
            indx++;
            BVHInScene[rootIndex].childrenIndex.Add(indx);
            if (workingListSon.Count < NumberOfMaxChildren + 1)   //if it contains 1 element -> this is a leaf
            {
                if (NumberOfMaxChildren == 1)
                {
                    son4.leaf = true;
                    son4.meshIndex = workingListSon[0].meshID;
                    son4.boundingBox = workingListSon[0].bb;
                }
                else
                {
                    int sonIndex = indx;
                    son4.leaf = false;
                    son4.empty = false;
                    son4.childrenIndex = new List<int>();
                    //change BB in order to fit all objects

                    son4.boundingBox = workingListSon[0].bb;
                    foreach (BVHElement b in workingListSon)
                    {
                        son4.boundingBox.Encapsulate(b.bb);
                        indx++;
                        son4.childrenIndex.Add(indx);
                        BoundingVolumeHierarchy leafCh = new BoundingVolumeHierarchy
                        {
                            fatherIndex = sonIndex,
                            leaf = true,
                            empty = false,
                            root = false,
                            boundingBox = b.bb,
                            meshIndex = b.meshID,
                        };
                        BVHInScene.Add(leafCh);
                    }
                }
                BVHInScene.Add(son4);
            }
            else    //if it contains >1 elements -> go deeper
            {
                son4.leaf = false;
                son4.empty = false;
                son4.childrenIndex = new List<int>();
                //change BB in order to fit all objects

                son4.boundingBox = workingListSon[0].bb;
                foreach (BVHElement b in workingListSon)
                {
                    son4.boundingBox.Encapsulate(b.bb);
                }
                son4.boundingBox.size += new Vector3(0.001f, 0.001f, 0.001f);

                BVHInScene.Add(son4);
                CreateBoundingVolumeHierarchy(workingListSon);
            }
        }
        else    //if not -> not use this son
        {
            son4.empty = true;
        }

        BoundingVolumeHierarchy son5 = new BoundingVolumeHierarchy();
        son5.fatherIndex = rootIndex;
        son5.root = false;
        centerBB = new Vector3((BVHInScene[rootIndex].boundingBox.center.x + BVHInScene[rootIndex].boundingBox.max.x) / 2, (BVHInScene[rootIndex].boundingBox.center.y + BVHInScene[rootIndex].boundingBox.min.y) / 2, (BVHInScene[rootIndex].boundingBox.center.z + BVHInScene[rootIndex].boundingBox.min.z) / 2);
        son5.boundingBox = new Bounds(centerBB, BVHInScene[rootIndex].boundingBox.extents + new Vector3(0.001f, 0.001f, 0.001f));

        //check if son1 contains anything
        workingListSon = new List<BVHElement>();
        for (int i = 0; i < workingList.Count; i++)
        {
            if (son5.boundingBox.Contains(workingList[i].bb.center))
            {
                //it contains something
                workingListSon.Add(workingList[i]);
                workingList.RemoveAt(i);
            }
        }

        if (workingListSon.Count > 0)   //if yes -> use this son
        {
            indx++;
            BVHInScene[rootIndex].childrenIndex.Add(indx);
            if (workingListSon.Count < NumberOfMaxChildren + 1)   //if it contains 1 element -> this is a leaf
            {
                if (NumberOfMaxChildren == 1)
                {
                    son5.leaf = true;
                    son5.meshIndex = workingListSon[0].meshID;
                    son5.boundingBox = workingListSon[0].bb;
                }
                else
                {
                    int sonIndex = indx;
                    son5.leaf = false;
                    son5.empty = false;
                    son5.childrenIndex = new List<int>();
                    //change BB in order to fit all objects

                    son5.boundingBox = workingListSon[0].bb;
                    foreach (BVHElement b in workingListSon)
                    {
                        son5.boundingBox.Encapsulate(b.bb);
                        indx++;
                        son5.childrenIndex.Add(indx);
                        BoundingVolumeHierarchy leafCh = new BoundingVolumeHierarchy
                        {
                            fatherIndex = sonIndex,
                            leaf = true,
                            empty = false,
                            root = false,
                            boundingBox = b.bb,
                            meshIndex = b.meshID,
                        };
                        BVHInScene.Add(leafCh);
                    }
                }
                BVHInScene.Add(son5);
            }
            else    //if it contains >1 elements -> go deeper
            {
                son5.leaf = false;
                son5.empty = false;
                son5.childrenIndex = new List<int>();
                //change BB in order to fit all objects

                son5.boundingBox = workingListSon[0].bb;
                foreach (BVHElement b in workingListSon)
                {
                    son5.boundingBox.Encapsulate(b.bb);
                }
                son5.boundingBox.size += new Vector3(0.001f, 0.001f, 0.001f);

                BVHInScene.Add(son5);
                CreateBoundingVolumeHierarchy(workingListSon);
            }
        }
        else    //if not -> not use this son
        {
            son5.empty = true;
        }

        BoundingVolumeHierarchy son6 = new BoundingVolumeHierarchy();
        son6.fatherIndex = rootIndex;
        son6.root = false;
        centerBB = new Vector3((BVHInScene[rootIndex].boundingBox.center.x + BVHInScene[rootIndex].boundingBox.max.x) / 2, (BVHInScene[rootIndex].boundingBox.center.y + BVHInScene[rootIndex].boundingBox.max.y) / 2, (BVHInScene[rootIndex].boundingBox.center.z + BVHInScene[rootIndex].boundingBox.min.z) / 2);
        son6.boundingBox = new Bounds(centerBB, BVHInScene[rootIndex].boundingBox.extents + new Vector3(0.001f, 0.001f, 0.001f));

        //check if son1 contains anything
        workingListSon = new List<BVHElement>();
        for (int i = 0; i < workingList.Count; i++)
        {
            if (son6.boundingBox.Contains(workingList[i].bb.center))
            {
                //it contains something
                workingListSon.Add(workingList[i]);
                workingList.RemoveAt(i);
            }
        }

        if (workingListSon.Count > 0)   //if yes -> use this son
        {
            indx++;
            BVHInScene[rootIndex].childrenIndex.Add(indx);
            if (workingListSon.Count < NumberOfMaxChildren + 1)   //if it contains 1 element -> this is a leaf
            {
                if (NumberOfMaxChildren == 1)
                {
                    son6.leaf = true;
                    son6.meshIndex = workingListSon[0].meshID;
                    son6.boundingBox = workingListSon[0].bb;
                }
                else
                {
                    int sonIndex = indx;
                    son6.leaf = false;
                    son6.empty = false;
                    son6.childrenIndex = new List<int>();
                    //change BB in order to fit all objects

                    son6.boundingBox = workingListSon[0].bb;
                    foreach (BVHElement b in workingListSon)
                    {
                        son6.boundingBox.Encapsulate(b.bb);
                        indx++;
                        son6.childrenIndex.Add(indx);
                        BoundingVolumeHierarchy leafCh = new BoundingVolumeHierarchy
                        {
                            fatherIndex = sonIndex,
                            leaf = true,
                            empty = false,
                            root = false,
                            boundingBox = b.bb,
                            meshIndex = b.meshID,
                        };
                        BVHInScene.Add(leafCh);
                    }
                }
                BVHInScene.Add(son6);
            }
            else    //if it contains >1 elements -> go deeper
            {
                son6.leaf = false;
                son6.empty = false;
                son6.childrenIndex = new List<int>();
                //change BB in order to fit all objects

                son6.boundingBox = workingListSon[0].bb;
                foreach (BVHElement b in workingListSon)
                {
                    son6.boundingBox.Encapsulate(b.bb);
                }

                son6.boundingBox.size += new Vector3(0.001f, 0.001f, 0.001f);
                BVHInScene.Add(son6);
                CreateBoundingVolumeHierarchy(workingListSon);
            }
        }
        else    //if not -> not use this son
        {
            son6.empty = true;
        }

        BoundingVolumeHierarchy son7 = new BoundingVolumeHierarchy();
        son7.fatherIndex = rootIndex;
        son7.root = false;
        centerBB = new Vector3((BVHInScene[rootIndex].boundingBox.center.x + BVHInScene[rootIndex].boundingBox.max.x) / 2, (BVHInScene[rootIndex].boundingBox.center.y + BVHInScene[rootIndex].boundingBox.min.y) / 2, (BVHInScene[rootIndex].boundingBox.center.z + BVHInScene[rootIndex].boundingBox.max.z) / 2);
        son7.boundingBox = new Bounds(centerBB, BVHInScene[rootIndex].boundingBox.extents + new Vector3(0.001f, 0.001f, 0.001f));

        //check if son1 contains anything
        workingListSon = new List<BVHElement>();
        for (int i = 0; i < workingList.Count; i++)
        {
            if (son7.boundingBox.Contains(workingList[i].bb.center))
            {
                //it contains something
                workingListSon.Add(workingList[i]);
                workingList.RemoveAt(i);
            }
        }

        if (workingListSon.Count > 0)   //if yes -> use this son
        {
            indx++;
            BVHInScene[rootIndex].childrenIndex.Add(indx);
            if (workingListSon.Count < NumberOfMaxChildren + 1)   //if it contains 1 element -> this is a leaf
            {
                if (NumberOfMaxChildren == 1)
                {
                    son7.leaf = true;
                    son7.meshIndex = workingListSon[0].meshID;
                    son7.boundingBox = workingListSon[0].bb;
                }
                else
                {
                    int sonIndex = indx;
                    son7.leaf = false;
                    son7.empty = false;
                    son7.childrenIndex = new List<int>();
                    //change BB in order to fit all objects

                    son7.boundingBox = workingListSon[0].bb;
                    foreach (BVHElement b in workingListSon)
                    {
                        son7.boundingBox.Encapsulate(b.bb);
                        indx++;
                        son7.childrenIndex.Add(indx);
                        BoundingVolumeHierarchy leafCh = new BoundingVolumeHierarchy
                        {
                            fatherIndex = sonIndex,
                            leaf = true,
                            empty = false,
                            root = false,
                            boundingBox = b.bb,
                            meshIndex = b.meshID,
                        };
                        BVHInScene.Add(leafCh);
                    }
                }
                BVHInScene.Add(son7);
            }
            else    //if it contains >1 elements -> go deeper
            {
                son7.leaf = false;
                son7.empty = false;
                son7.childrenIndex = new List<int>();
                //change BB in order to fit all objects

                son7.boundingBox = workingListSon[0].bb;
                foreach (BVHElement b in workingListSon)
                {
                    son7.boundingBox.Encapsulate(b.bb);
                }
                son7.boundingBox.size += new Vector3(0.001f, 0.001f, 0.001f);

                BVHInScene.Add(son7);
                CreateBoundingVolumeHierarchy(workingListSon);
            }
        }
        else    //if not -> not use this son
        {
            son7.empty = true;
        }

        BoundingVolumeHierarchy son8 = new BoundingVolumeHierarchy();
        son8.fatherIndex = rootIndex;
        son8.root = false;
        centerBB = new Vector3((BVHInScene[rootIndex].boundingBox.center.x + BVHInScene[rootIndex].boundingBox.max.x) / 2, (BVHInScene[rootIndex].boundingBox.center.y + BVHInScene[rootIndex].boundingBox.max.y) / 2, (BVHInScene[rootIndex].boundingBox.center.z + BVHInScene[rootIndex].boundingBox.max.z) / 2);
        son8.boundingBox = new Bounds(centerBB, BVHInScene[rootIndex].boundingBox.extents + new Vector3(0.001f, 0.001f, 0.001f));

        //check if son1 contains anything
        workingListSon = new List<BVHElement>();
        for (int i = 0; i < workingList.Count; i++)
        {
            if (son8.boundingBox.Contains(workingList[i].bb.center))
            {
                //it contains something
                workingListSon.Add(workingList[i]);
                workingList.RemoveAt(i);
            }
        }

        if (workingListSon.Count > 0)   //if yes -> use this son
        {
            indx++;
            BVHInScene[rootIndex].childrenIndex.Add(indx);
            if (workingListSon.Count < NumberOfMaxChildren + 1)   //if it contains 1 element -> this is a leaf
            {
                if (NumberOfMaxChildren == 1)
                {
                    son8.leaf = true;
                    son8.meshIndex = workingListSon[0].meshID;
                    son8.boundingBox = workingListSon[0].bb;
                }
                else
                {
                    int sonIndex = indx;
                    son8.leaf = false;
                    son8.empty = false;
                    son8.childrenIndex = new List<int>();
                    //change BB in order to fit all objects

                    son8.boundingBox = workingListSon[0].bb;
                    foreach (BVHElement b in workingListSon)
                    {
                        son8.boundingBox.Encapsulate(b.bb);
                        indx++;
                        son8.childrenIndex.Add(indx);
                        BoundingVolumeHierarchy leafCh = new BoundingVolumeHierarchy
                        {
                            fatherIndex = sonIndex,
                            leaf = true,
                            empty = false,
                            root = false,
                            boundingBox = b.bb,
                            meshIndex = b.meshID,
                        };
                        BVHInScene.Add(leafCh);
                    }
                }
                BVHInScene.Add(son8);
            }
            else    //if it contains >1 elements -> go deeper
            {
                son8.leaf = false;
                son8.empty = false;
                son8.childrenIndex = new List<int>();
                //change BB in order to fit all objects

                son8.boundingBox = workingListSon[0].bb;
                foreach (BVHElement b in workingListSon)
                {
                    son8.boundingBox.Encapsulate(b.bb);
                }
                son8.boundingBox.size += new Vector3(0.001f, 0.001f, 0.001f);

                BVHInScene.Add(son8);
                CreateBoundingVolumeHierarchy(workingListSon);
            }
        }
        else    //if not -> not use this son
        {
            son8.empty = true;
        }

        //add indexes of childern in root

        //Debug.Log("son 1 center" + son1.boundingBox.center + "  sonsize" + son1.boundingBox.size + "  son empty" + son1.empty);
        //Debug.Log("son 2 center" + son2.boundingBox.center + "  sonsize" + son2.boundingBox.size + "  son empty" + son2.empty);
        //Debug.Log("son 3 center" + son3.boundingBox.center + "  sonsize" + son3.boundingBox.size + "  son empty" + son3.empty);
        //Debug.Log("son 4 center" + son4.boundingBox.center + "  sonsize" + son4.boundingBox.size + "  son empty" + son4.empty);
        //Debug.Log("son 5 center" + son5.boundingBox.center + "  sonsize" + son5.boundingBox.size + "  son empty" + son5.empty);
        //Debug.Log("son 6 center" + son6.boundingBox.center + "  sonsize" + son6.boundingBox.size + "  son empty" + son6.empty);
        //Debug.Log("son 7 center" + son7.boundingBox.center + "  sonsize" + son7.boundingBox.size + "  son empty" + son7.empty);
        //Debug.Log("son 8 center" + son8.boundingBox.center + "  sonsize" + son8.boundingBox.size + "  son empty" + son8.empty);



        //BVHInScene[rootIndex];   //father




        //fare una lista con il priù grande come primo e dentro ogni oggetto della lista vengono indicati i figli (altri oggetti) o l'indice che definisce i figli

        //fare una lista ricorsiva con tutti i figli all'interno
    }


    private void CreateBoundingVolumeHierarchy2Sons(List<BVHElement> workingList)
    {
        int rootIndex = indx;


        BoundingVolumeHierarchy son1 = new BoundingVolumeHierarchy();
        son1.fatherIndex = rootIndex;
        son1.root = false;
        Vector3 centerBB = new Vector3((BVHInScene[rootIndex].boundingBox.center.x + BVHInScene[rootIndex].boundingBox.min.x) / 2, (BVHInScene[rootIndex].boundingBox.center.y + BVHInScene[rootIndex].boundingBox.min.y) / 2, (BVHInScene[rootIndex].boundingBox.center.z + BVHInScene[rootIndex].boundingBox.min.z) / 2);
        son1.boundingBox = new Bounds(centerBB, BVHInScene[rootIndex].boundingBox.extents + new Vector3(0.001f, 0.001f, 0.001f));

        //check if son1 contains anything
        List<BVHElement> workingListSon = new List<BVHElement>();


        for (int i = 0; i < workingList.Count; i++)
        {
            if (son1.boundingBox.Contains(workingList[i].bb.center))
            {
                //it contains something
                workingListSon.Add(workingList[i]);
                //workingList.RemoveAt(i);
            }
        }

        if (workingListSon.Count > 0)   //if yes -> use this son
        {
            indx++;
            BVHInScene[rootIndex].childrenIndex.Add(indx);
            if (workingListSon.Count < NumberOfMaxChildren + 1)   //if it contains 1 element -> this is a leaf
            {
                if (NumberOfMaxChildren == 1)
                {
                    son1.leaf = true;
                    son1.meshIndex = workingListSon[0].meshID;
                    son1.boundingBox = workingListSon[0].bb;
                }
                else
                {
                    int sonIndex = indx;
                    son1.leaf = false;
                    son1.empty = false;
                    son1.childrenIndex = new List<int>();
                    //change BB in order to fit all objects

                    son1.boundingBox = workingListSon[0].bb;
                    foreach (BVHElement b in workingListSon)
                    {
                        son1.boundingBox.Encapsulate(b.bb);
                        indx++;
                        son1.childrenIndex.Add(indx);
                        BoundingVolumeHierarchy leafCh = new BoundingVolumeHierarchy
                        {
                            fatherIndex = sonIndex,
                            leaf = true,
                            empty = false,
                            root = false,
                            boundingBox = b.bb,
                            meshIndex = b.meshID,
                        };
                        BVHInScene.Add(leafCh);
                    }
                }
                BVHInScene.Add(son1);
            }
            else    //if it contains >1 elements -> go deeper
            {
                son1.leaf = false;
                son1.empty = false;
                son1.childrenIndex = new List<int>();
                //change BB in order to fit all objects

                son1.boundingBox = workingListSon[0].bb;
                foreach (BVHElement b in workingListSon)
                {
                    son1.boundingBox.Encapsulate(b.bb);
                }
                son1.boundingBox.size += new Vector3(0.001f, 0.001f, 0.001f);
                BVHInScene.Add(son1);
                CreateBoundingVolumeHierarchy(workingListSon);
            }
        }
        else    //if not -> not use this son
        {
            son1.empty = true;
        }

    }



    private BoundingVolumeHierarchy CreateRootNode(List<BVHElement> bVHElements)
    {
        BoundingVolumeHierarchy rootNode = new BoundingVolumeHierarchy();

        rootNode.boundingBox = new Bounds(bVHElements[0].bb.center, new Vector3(0, 0, 0));

        foreach (BVHElement leaf in bVHElements)
        {
            rootNode.boundingBox.Encapsulate(leaf.bb);
        }
        rootNode.boundingBox.size += new Vector3(0.001f, 0.001f, 0.001f);
        rootNode.leaf = false;
        rootNode.root = true;
        rootNode.empty = false;
        rootNode.childrenIndex = new List<int>();
        return rootNode;
    }

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        foreach (BoundingVolumeHierarchy b in BVHInScene)
        {
            //if (b.isLeaf)
            //{
            //Debug.Log(b.level);
            Vector3 c = b.boundingBox.center;
            Vector3 s = b.boundingBox.size;
            //Gizmos.color = UnityEngine.Color.white;
            Gizmos.color = new UnityEngine.Color(0, 255, 255);
            Gizmos.DrawWireCube(c, s);
            //}
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}
