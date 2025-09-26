using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Linq;
using System.Runtime.ExceptionServices;

#if UNITY_EDITOR
using UnityEditor;
//using UnityEditor.Experimental.GraphView;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using static AABBox;

public class BVHconstruction : MonoBehaviour
{

    //rayHitBVH to do
    public int BVHdeep = 5;

    public static int[] AddItemToArray(int[] original, int itemToAdd)
    {
        int[] finalArray = new int[original.Length + 1];
        for (int i = 0; i < original.Length; i++)
        {
            finalArray[i] = original[i];
        }
        finalArray[finalArray.Length - 1] = itemToAdd;
        return finalArray;
    }
    public static List<AxisAlignBB>[] RemoveFromArray(List<AxisAlignBB>[] original, int itemToAdd)
    {
        List<AxisAlignBB>[] finalArray = new List<AxisAlignBB>[original.Length - 1];
        bool passed = false;
        for (int i = 0; i < finalArray.Length; i++)
        {
            if (i == itemToAdd)
                passed = true;

            if (passed)
            {
                finalArray[i] = original[i + 1];
            }
            else
            {
                finalArray[i] = original[i];
            }
        }
        return finalArray;
    }

    List<AxisAlignBB> WorkingList = new List<AxisAlignBB>();

    public BVHnode root;

    public List<BVHnode> BVH = new List<BVHnode>();

    public List<Bounds> BoundsForGizmo = new List<Bounds>();

    //check della BVH leaf

    private List<BVHnode> extractLeafsOnly(List<BVHnode> bVs)
    {
        List<BVHnode> leafsOver = new List<BVHnode>();

        foreach (BVHnode b in bVs)
        {
            if (b.isLeaf)
            {
                bool isNotIn = false;
                foreach (BVHnode bb in leafsOver)
                {
                    if (bb.BB.center == b.BB.center)
                        isNotIn = true;
                }
                if (!isNotIn)
                {
                    leafsOver.Add(b);
                }
            }
        }


        return leafsOver;
    }

    private List<AxisAlignBB> extractLeafsOnlyAA(List<AxisAlignBB> bVs)
    {
        List<AxisAlignBB> leafsOver = new List<AxisAlignBB>();

        foreach (AxisAlignBB b in bVs)
        {

            bool isNotIn = false;
            foreach (AxisAlignBB bb in leafsOver)
            {
                if (bb.BB.center.z == b.BB.center.z)
                    isNotIn = true;
            }
            if (!isNotIn)
            {
                leafsOver.Add(b);
            }
        }


        return leafsOver;
    }

    public struct BVHnode
    {
        //public List<BVHnode> nodesInsideThisNode;

        public Bounds BB;

        public TriangleInMesh tr;

        public bool isLeaf;
        public bool isRoot;

        public int level;


        public int indexOfNode;
        public List<int> childrenIndex;
        public int parentIndex;

        public int numberOfChildren;
        public int firstIndexOfArray;
    }

    public List<int> childsIndex;

    int indexforNode;

    bool test = true;

    List<AxisAlignBB> workingListNew;

    // Start is called before the first frame update
    void Start()
    {
        indexforNode = 0;

        CreateWorkingList();
        Debug.Log(WorkingList.Count);
        root = CreateRootNode(WorkingList);

        Debug.Log(root.BB.extents);

        //CreateBVH(root, WorkingList);
        //CreateBVHhalf(root, WorkingList, 1, true);


        BVH = BVHbuildNotRec(root, transformAABBinLeafs(WorkingList));

        Debug.Log("number Boxes in BVH: " + BVH.Count);

        foreach (BVHnode n in BVH)
        {
            Debug.Log("is it a leaf?   " + n.isLeaf);
            Debug.Log("numberchilds: " + n.numberOfChildren);
            Debug.Log("number level: " + n.level);
        }

        //Debug.Log("boxes " + BVH.Count);

        //Debug.Log("lastIndex " + BVH[BVH.Count - 1].indexOfNode);

        //Debug.Log(childsIndex.Count);

        //BVHnode testNode = new BVHnode(); ;
        //bool assigned = false;
        //foreach (BVHnode n in BVH)
        //{
        //    Debug.Log(n.numberOfChildren);
        //    if (n.isLeaf)
        //        Debug.Log("leaf");
        //    else
        //    {
        //        if (!assigned)
        //        {
        //            assigned = true;
        //            testNode = n;
        //        }
        //    }
        //}


        //BVHnode testNode = BVH[BVH.Count-8];

        //int firstId = testNode.firstIndexOfArray;

        //int numbChidl = testNode.numberOfChildren;

        //Debug.Log(numbChidl);

        //List<BVHnode> childenOfTest = new List<BVHnode>();


        //for (int i = 0; i < numbChidl - 1; i++)
        //{
        //    childenOfTest.Add(BVH[childsIndex[firstId + i - numbChidl + 1]]);
        //    Debug.Log(childsIndex[firstId + i - numbChidl + 1]);
        //}

        //Debug.Log(childenOfTest.Count);

        //Debug.Log(testNode.indexOfNode);

        //foreach (BVHnode n in childenOfTest)
        //{
        //    Debug.Log(n.parentIndex);
        //}

        //foreach (int n in testNode.childrenIndex)
        //{
        //    Debug.Log(n);
        //}

        //workingListNew = extractLeafsOnlyAA(WorkingList);

        //Debug.Log(workingListNew.Count);

        //List<BVHnode> leafsOut = extractLeafsOnly(BVH);

        //Debug.Log(leafsOut.Count);

        //Debug.Log("bit root: " + System.Runtime.InteropServices.Marshal.SizeOf(typeof(BVHnode)));   //bit of a certain node

    }

    private List<BVHnode> transformAABBinLeafs(List<AxisAlignBB> wl)
    {
        List<BVHnode> outBVH = new List<BVHnode>();

        foreach (AxisAlignBB aabb in wl)
        {
            BVHnode n = new BVHnode()
            {
                BB = aabb.BB,
                tr = aabb.triangle,
                isRoot = false,
                isLeaf = true,
            };
            outBVH.Add(n);
        }

        return outBVH;
    }

    private List<BVHnode> BVHbuildNotRec(BVHnode r, List<BVHnode> wList)
    {
        int idOfNode = 0;

        List<int> numberOfId = new List<int>();     //take the index and the id-numbchildren are the indexes of the children


        r.indexOfNode = idOfNode;



        //prima di inserire r controllare che tutti i parametri siano ok

        List<BVHnode> outBVH = new List<BVHnode>()
        {
        };
        //bool createdBVH = true;

        Bounds rightChild = new Bounds();
        Bounds leftChild = new Bounds();

        int cutAx = 0;

        (rightChild, leftChild) = CutBBintwoBounds(r.BB, cutAx);
        //int t = 0;

        leafBVH(r, rightChild, leftChild, wList);

        void leafBVH(BVHnode rootBVH, Bounds rightBound, Bounds leftBound, List<BVHnode> wl)
        {
            //t++;
            //Debug.Log("here 281 " + t);
            //Debug.Log("here 282 " + rootBVH.level);


            //Debug.Log("BVH numb: " + outBVH.Count); 

            Debug.Log("total: " + wl.Count);

            if (rootBVH.level > 10)
            {
                Debug.LogWarning("help level > 10 ");
            }

            if (wl.Count < 5)
            {
                Debug.LogError("non dovrei essere qui");
            }

            //cutAx = UnityEngine.Random.Range(0,2);
            cutAx++;
            if (cutAx > 2)
                cutAx = 0;


            List<BVHnode> leftLeafs = new List<BVHnode>();
            List<BVHnode> rightLeafs = new List<BVHnode>();

            BVHnode r1 = new BVHnode()
            {
                BB = rightBound,
                isLeaf = false,
                isRoot = false,
                level = rootBVH.level + 1,
                indexOfNode = 0,
                parentIndex = rootBVH.indexOfNode,
                numberOfChildren = 0,

            };

            BVHnode r2 = new BVHnode()
            {
                BB = leftBound,
                isLeaf = false,
                isRoot = false,
                level = r.level + 1,
                indexOfNode = 0,
                parentIndex = rootBVH.indexOfNode,
                numberOfChildren = 0,

            };

            //cut in the best way (maximize leaf count)

            bool goOut = false;
            int tries = 0;

            while (!goOut)
            {
                rightLeafs.Clear();
                leftLeafs.Clear();

                foreach (BVHnode leaf in wl)
                {
                    if (r1.BB.Intersects(leaf.BB))
                    {
                        //leaf is in r1
                        rightLeafs.Add(leaf);

                    }
                    if (r2.BB.Intersects(leaf.BB))
                    {
                        //leaf is in r2
                        leftLeafs.Add(leaf);
                    }
                }

                List<BVHnode> tempOutArr = new List<BVHnode>();
                //check if there are same BB in the two arrays
                foreach (BVHnode l in rightLeafs)
                {
                    for (int i = 0; i < leftLeafs.Count; i++)
                    {
                        //if ((l.tr.localToWMat*l.tr.p1 == leftLeafs[i].tr.localToWMat * leftLeafs[i].tr.p1) && (l.tr.localToWMat * l.tr.p2 == leftLeafs[i].tr.localToWMat * leftLeafs[i].tr.p2) && (l.tr.localToWMat * l.tr.p3 == leftLeafs[i].tr.localToWMat * leftLeafs[i].tr.p3))
                        if (l.BB.center == leftLeafs[i].BB.center && l.BB.size == leftLeafs[i].BB.size)
                        {
                            tempOutArr.Add(l);
                            Debug.LogWarning("è lo stesso triangolo");
                        }
                    }
                }

                foreach (BVHnode n in tempOutArr)
                {
                    if (UnityEngine.Random.Range(0.0f,1.0f) > 0.5f)
                        rightLeafs.Remove(n);
                    else
                        leftLeafs.Remove(n);
                }


                if (rightLeafs.Count + leftLeafs.Count < wl.Count || rightLeafs.Count + leftLeafs.Count > wl.Count || rightLeafs.Count < 1 || leftLeafs.Count < 1)
                {
                    (rightBound, leftBound) = CutBBintwoBounds(rootBVH.BB, cutAx);
                    r1.BB = rightBound;
                    r2.BB = leftBound;
                    tries++;
                    cutAx++;
                    if (cutAx > 2)
                        cutAx = 0;
                }
                else
                {
                    //Debug.Log("done");
                    goOut = true;
                }

                if (tries > 3)
                {
                    Debug.LogError("not done");
                    goOut = true;
                }

            }

            Debug.Log("left nodes: " + leftLeafs.Count);
            Debug.Log("right nodes: " + rightLeafs.Count);




            //REMODULATE SIZE OF r1.BB AND r2.BB

            //Debug.Log("sizeBefore"+r1.BB.size);

            if (rightLeafs.Count > 0)
            {
                //Debug.Log("size first leaf: " + rightLeafs[0].BB.size);
                r1 = ResizeBBofRoot(r1, rightLeafs);
                //Debug.Log("sizeAfter" + r1.BB.size);
            }
            if (leftLeafs.Count > 0)
                r2 = ResizeBBofRoot(r2, leftLeafs);

            //newBounds
            Bounds BBrightch;
            Bounds BBleftch;

            if (rightLeafs.Count > 0 && rightLeafs.Count < 5)
            {
                idOfNode++;
                r1.indexOfNode = idOfNode;
                for (int i = 0; i < rightLeafs.Count; i++)
                {
                    r1.numberOfChildren++;
                    BVHnode temp = rightLeafs[i];
                    idOfNode++;
                    rightLeafs[i] = new BVHnode()
                    {
                        BB = temp.BB,
                        tr = temp.tr,
                        indexOfNode = idOfNode,
                        isLeaf = temp.isLeaf,
                        isRoot = temp.isRoot,
                        level = r1.level + 1,
                        numberOfChildren = 0,
                        parentIndex = r1.indexOfNode,
                    };

                    numberOfId.Add(idOfNode);
                    outBVH.Add(rightLeafs[i]);
                }

                numberOfId.Add(idOfNode);
                outBVH.Add(r1);
            }
            else if (rightLeafs.Count >= 5)
            {
                //Debug.Log("number in right: " + rightLeafs.Count);
                //PER R1
                rootBVH.numberOfChildren++;
                idOfNode++;
                r1.indexOfNode = idOfNode;


                (BBrightch, BBleftch) = CutBBintwoBounds(r1.BB, cutAx);        //i tagli vanno fatti su assi indicativi, non a caso
                leafBVH(r1, BBrightch, BBleftch, rightLeafs);


                numberOfId.Add(idOfNode);
                outBVH.Add(r1);
            }

            if (leftLeafs.Count > 0 && leftLeafs.Count < 5)
            {
                idOfNode++;
                r2.indexOfNode = idOfNode;
                for (int i = 0; i < leftLeafs.Count; i++)
                {
                    r2.numberOfChildren++;
                    BVHnode temp = leftLeafs[i];
                    idOfNode++;
                    leftLeafs[i] = new BVHnode()
                    {
                        BB = temp.BB,
                        tr = temp.tr,
                        indexOfNode = idOfNode,
                        isLeaf = temp.isLeaf,
                        isRoot = temp.isRoot,
                        level = r2.level + 1,
                        numberOfChildren = 0,
                        parentIndex = r2.indexOfNode,
                    };


                    numberOfId.Add(idOfNode);
                    outBVH.Add(leftLeafs[i]);
                }


                numberOfId.Add(idOfNode);
                outBVH.Add(r2);
            }
            else if (leftLeafs.Count >= 5)
            {
                //Debug.Log("number in left: " + leftLeafs.Count);
                //PER R2
                rootBVH.numberOfChildren++;
                idOfNode++;
                r2.indexOfNode = idOfNode;

                (BBrightch, BBleftch) = CutBBintwoBounds(r2.BB, cutAx);        //i tagli vanno fatti su assi indicativi, non a caso
                leafBVH(r2, BBrightch, BBleftch, leftLeafs);


                numberOfId.Add(idOfNode);
                outBVH.Add(r2);
            }
        }

        outBVH.Add(r);

        return outBVH;
    }

    private BVHnode ResizeBBofRoot(BVHnode r, List<BVHnode> Leafs)
    {
        BVHnode temp = r;
        Bounds b = Leafs[0].BB;
        //Debug.Log("b size: "+b.size);
        foreach (BVHnode n in Leafs)
        {
            b.Encapsulate(n.BB);
        }
        //Debug.Log("b size: "+b.size);
        temp.BB = b;
        return temp;
    }

    private (Bounds childLeft, Bounds childRight) CutBBintwoBounds(Bounds b, int axis)      //0 = x   1 = y   2 = z
    {
        Debug.Log("asse: " + axis);

        Vector3 centerBL = new Vector3();
        Vector3 centerBR = new Vector3();
        Vector3 SizeB = new Vector3();
        switch (axis)
        {
            case 0:
                //if cut on X
                centerBL = b.center;
                centerBL.x = (b.center.x + b.min.x) / 2;
                centerBR = b.center;
                centerBR.x = (b.center.x + b.max.x) / 2;
                SizeB = b.size;
                SizeB.x /= 2;

                break;
            case 1:
                //if cut on Y
                centerBL = b.center;
                centerBL.y = (b.center.y + b.min.y) / 2;
                centerBR = b.center;
                centerBR.y = (b.center.y + b.max.y) / 2;
                SizeB = b.size;
                SizeB.y /= 2;
                break;
            case 2:
                //if cut on Z
                centerBL = b.center;
                centerBL.z = (b.center.z + b.min.z) / 2;
                centerBR = b.center;
                centerBR.z = (b.center.z + b.max.z) / 2;
                SizeB = b.size;
                SizeB.z /= 2;
                break;
        }
        Bounds cL = new Bounds(centerBL, SizeB);
        Bounds cR = new Bounds(centerBR, SizeB);

        //cL.size -= new Vector3(0.0001f, 0.0001f, 0.0001f);
        //cR.size -= new Vector3(0.0001f, 0.0001f, 0.0001f);

        return (cL, cR);
    }

    private void CheckIfWlistisInRightOrLeft(List<AxisAlignBB> workingList, Bounds childLeft, Bounds childRight, List<AxisAlignBB> leftWList, List<AxisAlignBB> rightWList)
    {
        foreach (AxisAlignBB b in workingList)
        {
            if (childLeft.Intersects(b.BB))
                leftWList.Add(b);
            if (childRight.Intersects(b.BB))
                rightWList.Add(b);
        }
    }

    private void CreateBVHhalf(BVHnode r, List<AxisAlignBB> workingList, int oldIndexAx, bool firstTime)
    {
        Debug.Log("working list IN -number element- : " + workingList.Count);

        //gli elementi in workinglist vengono suddivisi nei due figli di r se in childLeft vanno nel working list left e poi in child left, se in childRight vanno nel working list right e poi in child right 
        Bounds childLeft = new Bounds();
        Bounds childRight = new Bounds();

        List<AxisAlignBB> leftWList = new List<AxisAlignBB>();
        List<AxisAlignBB> rightWList = new List<AxisAlignBB>();

        int newindex = 0;

        (childLeft, childRight) = CutBBintwoBounds(r.BB, oldIndexAx);

        newindex = oldIndexAx + 1;

        if (newindex > 2)
            newindex = 0;

        //childLeft.size += new Vector3(0.001f, 0.001f, 0.001f);
        //childRight.size += new Vector3(0.001f, 0.001f, 0.001f);


        CheckIfWlistisInRightOrLeft(workingList, childLeft, childRight, leftWList, rightWList);

        if (leftWList.Count + rightWList.Count > workingList.Count)
        {
            leftWList.Clear();
            rightWList.Clear();

            (childLeft, childRight) = CutBBintwoBounds(r.BB, newindex);

            newindex = oldIndexAx + 1;

            if (newindex > 2)
                newindex = 0;

            //childLeft.size += new Vector3(0.001f, 0.001f, 0.001f);
            //childRight.size += new Vector3(0.001f, 0.001f, 0.001f);


            CheckIfWlistisInRightOrLeft(workingList, childLeft, childRight, leftWList, rightWList);

        }

        if (leftWList.Count + rightWList.Count > workingList.Count)
        {
            leftWList.Clear();
            rightWList.Clear();

            newindex += 1;

            if (newindex > 2)
                newindex = 0;

            (childLeft, childRight) = CutBBintwoBounds(r.BB, newindex);

            newindex = oldIndexAx + 1;

            if (newindex > 2)
                newindex = 0;

            childLeft.size += new Vector3(0.001f, 0.001f, 0.001f);
            childRight.size += new Vector3(0.001f, 0.001f, 0.001f);


            CheckIfWlistisInRightOrLeft(workingList, childLeft, childRight, leftWList, rightWList);

        }
        Debug.Log("working list RIGHT -number element- : " + rightWList.Count);
        Debug.Log("working list LEFT -number element- : " + leftWList.Count);


        //resize the AABB
        childLeft.size = new Vector3(0, 0, 0);
        childLeft.center = leftWList[0].BB.center;
        childRight.size = new Vector3(0, 0, 0);
        childRight.center = rightWList[0].BB.center;

        foreach (AxisAlignBB b in leftWList)
        {
            childLeft.Encapsulate(b.BB);
        }

        foreach (AxisAlignBB b in rightWList)
        {
            childRight.Encapsulate(b.BB);
        }

        BoundsForGizmo.Add(childLeft);
        BoundsForGizmo.Add(childRight);

        //ora che i BB sono al giusto size posso farli diventare una radice per un nuovo livello e salvare quella radice
        if (leftWList.Count > 0)
        {
            if (leftWList.Count < 6)
            {
                //ci sono solo 4 elementi nel nodo r e quei 3 elementi diventano tutti foglie
                //poi return
                foreach (AxisAlignBB b in leftWList)
                {
                    r.numberOfChildren++;
                    indexforNode++;
                    int indexPort = indexforNode;
                    r.childrenIndex.Add(indexPort);
                    childsIndex.Add(indexPort);
                    BVHnode leaf = new BVHnode()
                    {
                        BB = b.BB,
                        isLeaf = true,
                        isRoot = false,
                        tr = b.triangle,
                        level = r.level + 1,
                        parentIndex = r.indexOfNode,
                        indexOfNode = indexPort,
                        childrenIndex = new List<int>(),
                        numberOfChildren = 0,
                    };
                    BVH.Add(leaf);
                }
            }
            else
            {
                r.numberOfChildren++;
                indexforNode++;
                int indexPort = indexforNode;
                Debug.Log(indexPort);

                r.childrenIndex.Add(indexPort);
                childsIndex.Add(indexPort);
                //left BB
                BVHnode leftBB = new BVHnode()
                {
                    BB = childLeft,
                    isLeaf = false,
                    isRoot = false,
                    level = r.level + 1,
                    parentIndex = r.indexOfNode,
                    indexOfNode = indexPort,
                    childrenIndex = new List<int>(),
                    firstIndexOfArray = indexPort,
                    numberOfChildren = 0,
                };
                //Debug.Log(leftBB.parentIndex);

                CreateBVHhalf(leftBB, leftWList, newindex, false);
            }
        }


        if (rightWList.Count > 0)
        {
            if (rightWList.Count < 6)
            {
                //ci sono solo 4 elementi nel nodo r e quei 3 elementi diventano tutti foglie
                //poi return
                foreach (AxisAlignBB b in rightWList)
                {

                    r.numberOfChildren++;
                    indexforNode++;
                    int indexPort = indexforNode;
                    r.childrenIndex.Add(indexPort);
                    childsIndex.Add(indexPort);
                    BVHnode leaf = new BVHnode()
                    {
                        BB = b.BB,
                        isLeaf = true,
                        isRoot = false,
                        tr = b.triangle,
                        level = r.level + 1,
                        parentIndex = r.indexOfNode,
                        indexOfNode = indexPort,
                        childrenIndex = new List<int>(),
                        numberOfChildren = 0,
                    };
                    BVH.Add(leaf);
                }
            }
            else
            {

                r.numberOfChildren++;
                indexforNode++;
                int indexPort = indexforNode;
                Debug.Log(indexPort);
                r.childrenIndex.Add(indexPort);
                childsIndex.Add(indexPort);

                BVHnode rightBB = new BVHnode()
                {
                    BB = childRight,
                    isLeaf = false,
                    isRoot = false,
                    level = r.level + 1,
                    parentIndex = r.indexOfNode,
                    indexOfNode = indexPort,
                    childrenIndex = new List<int>(),
                    firstIndexOfArray = indexPort,
                    numberOfChildren = 0,
                };

                //Debug.Log(rightBB.parentIndex);
                CreateBVHhalf(rightBB, rightWList, newindex, false);
            }
        }

        if (rightWList.Count < 1 && leftWList.Count < 1)
        {
            r.isLeaf = true;
        }

        BVH.Add(r);
    }



    private void CreateBVH(BVHnode r, List<AxisAlignBB> workingList)
    {
        Debug.Log("working list input: " + workingList.Count);

        if (workingList.Count < 6)
        {
            foreach (AxisAlignBB bb in workingList)
            {
                BVHnode n = new BVHnode()
                {
                    BB = bb.BB,
                    level = BVHdeep,
                    isLeaf = true,
                    isRoot = false,
                    tr = bb.triangle,
                    parentIndex = r.indexOfNode,
                    indexOfNode = indexforNode,
                };
                r.numberOfChildren++;
                BVH.Add(n);
            }
            BVH.Add(r);
            return;
        }

        r.firstIndexOfArray = childsIndex.Count;

        List<BVHnode> nodesInNode = new List<BVHnode>();

        BVHnode n1 = new BVHnode() { BB = new Bounds((r.BB.min + r.BB.center) / 2, r.BB.extents) };
        BVHnode n2 = new BVHnode() { BB = new Bounds((r.BB.max + r.BB.center) / 2, r.BB.extents) };
        BVHnode n3 = new BVHnode() { BB = new Bounds(new Vector3(n1.BB.center.x, n1.BB.center.y, n2.BB.center.z), r.BB.extents) };
        BVHnode n4 = new BVHnode() { BB = new Bounds(new Vector3(n2.BB.center.x, n2.BB.center.y, n1.BB.center.z), r.BB.extents) };
        BVHnode n5 = new BVHnode() { BB = new Bounds(new Vector3(n1.BB.center.x, n2.BB.center.y, n2.BB.center.z), r.BB.extents) };
        BVHnode n6 = new BVHnode() { BB = new Bounds(new Vector3(n2.BB.center.x, n1.BB.center.y, n1.BB.center.z), r.BB.extents) };
        BVHnode n7 = new BVHnode() { BB = new Bounds(new Vector3(n1.BB.center.x, n2.BB.center.y, n1.BB.center.z), r.BB.extents) };
        BVHnode n8 = new BVHnode() { BB = new Bounds(new Vector3(n2.BB.center.x, n1.BB.center.y, n2.BB.center.z), r.BB.extents) };

        nodesInNode.Add(n1);
        nodesInNode.Add(n2);
        nodesInNode.Add(n3);
        nodesInNode.Add(n4);
        nodesInNode.Add(n5);
        nodesInNode.Add(n6);
        nodesInNode.Add(n7);
        nodesInNode.Add(n8);

        List<AxisAlignBB>[] workingLists = new List<AxisAlignBB>[8] {
            new List<AxisAlignBB>(),
            new List<AxisAlignBB>(),
            new List<AxisAlignBB>(),
            new List<AxisAlignBB>(),
            new List<AxisAlignBB>(),
            new List<AxisAlignBB>(),
            new List<AxisAlignBB>(),
            new List<AxisAlignBB>()
        };

        r.childrenIndex = new List<int>();

        foreach (AxisAlignBB b in workingList)
        {
            int li = 0;
            foreach (BVHnode n in nodesInNode)
            {

                if (n.BB.Contains(b.BB.center))
                    workingLists[li].Add(b);
                li++;
            }

        }
        //Debug.Log("number w list: " + workingLists.Length);
        //for (int i = 0; i < workingLists.Length; i++)
        //    Debug.Log(workingLists[i].Count);

        int[] indexs = new int[0];
        for (int i = 0; i < 8; i++)
        {
            if (workingLists[i].Count < 1)
            {
                indexs = AddItemToArray(indexs, i);
            }
        }

        if (indexs.Length > 7)
        {
            return;
        }

        int k = 0;
        if (indexs.Length > 0)
        {
            foreach (int i in indexs)
            {
                workingLists = RemoveFromArray(workingLists, i - k);
                nodesInNode.RemoveAt(i - k);
                k++;
            }
        }

        //a questo punto le wlist sono > 0 e minori o uguali a 8
        //anche la lista nodesInNode

        //salvo solo le root e le leaf non i passaggi intermedi (i passaggi intermedi sono le nuove root)

        //if (r.level > BVHdeep - 1)
        //{
        //    //se il livello è > 5 la root sarà l'ultima e tutto il resto saranno foglie
        //    for (int nList = 0; nList < workingLists.Length; nList++)
        //    {
        //        foreach (AxisAlignBB bb in workingLists[nList])
        //        {

        //            Debug.Log("isALeaf");
        //            //queste saranno tutte foglie
        //            r.numberOfChildren++;
        //            indexforNode++;
        //            uint indexPort = indexforNode;
        //            r.childrenIndex.Add(indexPort);
        //            childsIndex.Add(indexPort);
        //            BVHnode n = new BVHnode()
        //            {
        //                BB = bb.BB,
        //                level = BVHdeep,
        //                isLeaf = true,
        //                isRoot = false,
        //                tr = bb.triangle,
        //                parentIndex = r.indexOfNode,
        //                indexOfNode = indexforNode,
        //            };
        //            BVH.Add(n);
        //            //r.nodesInsideThisNode.Add(n);
        //        }
        //    }
        //    //salvo la radice di queste foglie
        //    BVH.Add(r);
        //    return;
        //}



        //Debug.Log("number w list without 0 size: " + workingLists.Length);

        for (int i = 0; i < workingLists.Length; i++)
        {
            if (workingLists[i].Count < 1)
            {
                return;
                //non ci sono elementi nella Wlist  //questo caso non esiste controllato a riga 188
            }
            else if (workingLists[i].Count > 0 && workingLists[i].Count < 2)
            {
                Debug.Log("isALeaf");

                //c'è un elemento nella working list del sottonodo quindi quell'unico elemento è foglia di questa radice

                //queste saranno tutte foglie
                r.numberOfChildren++;
                indexforNode++;
                int indexPort = indexforNode;
                r.childrenIndex.Add(indexPort);
                childsIndex.Add(indexPort);
                BVHnode n = new BVHnode()
                {
                    BB = workingLists[i][0].BB,
                    level = r.level + 1,
                    isLeaf = true,
                    isRoot = false,
                    tr = workingLists[i][0].triangle,
                    parentIndex = r.indexOfNode,
                    indexOfNode = indexforNode,
                };
                BVH.Add(n);
                //r.nodesInsideThisNode.Add(n);
            }
            else
            {
                Debug.Log("nuova BVHnode");
                //ci sono tanti elementi nella sottolist del sottonodo, bisogna salvarlo come nodo grande e rimandare questo nodo con la sua wList
                //il nuovo nodo che farà da radice ai prossimi sarà di una misura adeguata:
                Bounds tempBB = new Bounds(workingLists[i][0].BB.center, new Vector3(0, 0, 0));
                foreach (AxisAlignBB b in workingLists[i])
                {
                    tempBB.Encapsulate(b.BB);
                    //Debug.Log(tempBB);
                }
                r.numberOfChildren++;
                indexforNode++;
                int indexPort = indexforNode;
                r.childrenIndex.Add(indexPort);
                childsIndex.Add(indexPort);
                BVHnode newRoot = new BVHnode()
                {
                    BB = tempBB,
                    level = r.level + 1,
                    isLeaf = false,
                    isRoot = false,
                    //nodesInsideThisNode = new List<BVHnode>(),
                    parentIndex = r.indexOfNode,
                    indexOfNode = indexforNode,
                    numberOfChildren = 0,
                };


                if (test)
                {
                    CreateBVH(newRoot, workingLists[i]);
                }

            }
        }
        BVH.Add(r);
    }

    void OnDrawGizmosSelected()
    {
        //// A sphere that fully encloses the bounding box.
        //Bounds n1 = new Bounds((root.BB.min + root.BB.center) / 2, root.BB.extents);
        //Bounds n2 =new Bounds((root.BB.max + root.BB.center) / 2, root.BB.extents);
        //Bounds n3 = new Bounds(new Vector3(n1.center.x, n1.center.y, n2.center.z), root.BB.extents) ;
        //Bounds n4 =new Bounds(new Vector3(n2.center.x, n2.center.y, n1.center.z), root.BB.extents) ;
        //Bounds n5 =new Bounds(new Vector3(n1.center.x, n2.center.y, n2.center.z), root.BB.extents) ;
        //Bounds n6 =new Bounds(new Vector3(n2.center.x, n1.center.y, n1.center.z), root.BB.extents) ;
        //Bounds n7 =new Bounds(new Vector3(n1.center.x, n2.center.y, n1.center.z), root.BB.extents) ;
        //Bounds n8 =new Bounds(new Vector3(n2.center.x, n1.center.y, n2.center.z), root.BB.extents) ;

        //Gizmos.color = Color.green;
        //Gizmos.DrawWireCube(n1.center, n1.size);
        //Gizmos.DrawWireCube(n2.center, n2.size);
        //Gizmos.DrawWireCube(n3.center, n3.size);
        //Gizmos.DrawWireCube(n4.center, n4.size);
        //Gizmos.DrawWireCube(n5.center, n5.size);
        //Gizmos.DrawWireCube(n6.center, n6.size);
        //Gizmos.DrawWireCube(n7.center, n7.size);
        //Gizmos.DrawWireCube(n8.center, n8.size);


        foreach (BVHnode b in BVH)
        {
            //if (b.isLeaf)
            //{
            //Debug.Log(b.level);
            Vector3 c = b.BB.center;
            Vector3 s = b.BB.size;
            //Gizmos.color = UnityEngine.Color.white;
            Gizmos.color = new UnityEngine.Color(b.level * 30, 255, 255);
            Gizmos.DrawWireCube(c, s);
            //}
        }


        //foreach (Bounds b in BoundsForGizmo)
        //{
        //    Vector3 c = b.center;
        //    Vector3 s = b.size;
        //    Gizmos.color = UnityEngine.Color.green;
        //    Gizmos.DrawWireCube(c, s);
        //}

        //foreach (AxisAlignBB b in workingListNew)
        //{
        //    Vector3 c = b.BB.center;
        //    Vector3 s = b.BB.size;
        //    Gizmos.color = UnityEngine.Color.yellow;
        //    Gizmos.DrawWireCube(c, s);
        //}


        Vector3 center = root.BB.center;
        Vector3 size = root.BB.size;
        Gizmos.color = UnityEngine.Color.red;
        Gizmos.DrawWireCube(center, size);

    }

    private BVHnode CreateRootNode(List<AxisAlignBB> workingList)
    {
        BVHnode rootNode = new BVHnode();

        rootNode.BB = new Bounds(workingList[0].BB.center, new Vector3(0, 0, 0));

        foreach (AxisAlignBB leaf in workingList)
        {
            rootNode.BB.Encapsulate(leaf.BB);
        }
        rootNode.BB.size += new Vector3(0.001f, 0.001f, 0.001f);
        rootNode.isLeaf = false;
        rootNode.isRoot = true;
        rootNode.level = 0;

        //rootNode.nodesInsideThisNode = new List<BVHnode>();
        rootNode.childrenIndex = new List<int>();
        rootNode.indexOfNode = indexforNode;
        rootNode.parentIndex = 0;
        rootNode.numberOfChildren = 0;
        rootNode.firstIndexOfArray = 0;

        //BVH.Add(rootNode);

        return rootNode;
    }

    private void CreateWorkingList()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        Debug.Log(allObjects.Length);
        foreach (GameObject go in allObjects)
        {
            if (go.GetComponent<AABBox>() != null)
            {
                List<AxisAlignBB> l = go.GetComponent<AABBox>().aabbS;
                foreach (AxisAlignBB a in l)
                {
                    WorkingList.Add(a);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
