﻿#if PEGI

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System;
using System.Linq;

using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using SharedTools_Stuff;

namespace PlayerAndEditorGUI
{



    public interface iPEGI
    {
        bool PEGI();
    }

    public interface iGotName
    {
        string NameForPEGI { get; set; }
    }

    public interface iGotDisplayName
    {
        string NameForPEGIdisplay(); 
    }

   

    public interface iGotIndex
    {
        int GetIndex();
        void SetIndex(int index);
    }

    public delegate bool PEGIcallDelegate();

    public static class pegi
    {

        public class windowPositionData
        {
            public windowFunction funk;
            public Rect windowRect;

            public void drawFunction(int windowID)
            {
                paintingPlayAreaGUI = true;

                elementIndex = 0;
                lineOpen = false;
                focusInd = 0;

                GUI.backgroundColor = Color.white;

                funk();

                mouseOverUI = windowRect.Contains(Input.mousePosition);

                newLine();
                GUI.DragWindow(new Rect(0, 0, 10000, 20));
                newLine();
                GUI.color = Color.white;
                ("Tip:" + GUI.tooltip).nl();

                paintingPlayAreaGUI = false;
            }

            public void Render(windowFunction doWindow, string c_windowName)
            {
                funk = doWindow;
                windowRect = GUILayout.Window(0, windowRect, drawFunction, c_windowName);
            }

            public void collapse()
            {
                windowRect.width = 10;
                windowRect.height = 10;
            }

            public windowPositionData()
            {
                windowRect = new Rect(20, 20, 120, 400);
            }
        }

        public delegate bool windowFunction();
        //int windowID
        // This class is an attempt to enable creation of component inspectors that can also render to the screen. 
        static int elementIndex;
        static int focusInd;

        public static bool isFoldedOut = false;
        public static bool mouseOverUI = false;

        static int selectedFold = -1;
        public static int tabIndex; // will be reset on every NewLine;
        public static bool paintingPlayAreaGUI { get; private set; }


        static bool lineOpen;

        public static bool inspect<T>(this T o, object so) where T : MonoBehaviour, iPEGI {
            #if UNITY_EDITOR
                return ef.inspect(o, (SerializedObject)so);
            #else
             "PEGI is compiled without UNITY_EDITOR directive".nl();
                return false;
            #endif
        }

        public static bool inspect_so<T>(this T o, object so) where T : ScriptableObject, iPEGI {
            #if UNITY_EDITOR
            return ef.inspect_so(o, (SerializedObject)so);
            #else
             "PEGI is compiled without UNITY_EDITOR directive".nl();
                return false;
            #endif
        }

        public static void newLine()
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.newLine();
            }
            else
#endif
        if (lineOpen)
            {
                lineOpen = false;
                GUILayout.EndHorizontal();
            }
        }

        public static void nl()
        {
            newLine();
        }

        public static bool nl(this bool value)
        {
            newLine();
            return value;
        }

        public static bool nl(this string value)
        {
            write(value);
            newLine();
            return false;
        }

        public static bool nl(this string value, string tip)
        {
            write(value, tip);
            newLine();
            return false;
        }

        public static bool nl(this string value, int width)
        {
            write(value, width);
            newLine();
            return false;
        }

        public static bool nl(this string value, string tip, int width)
        {
            write(value, tip, width);
            newLine();
            return false;
        }

        public static void nl(this icon icon, int size)
        {
            pegi.write(icon.getIcon(), size);
        }

        public static void checkLine()
        {



#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {

                ef.checkLine();
            }
            else
#endif
        if (!lineOpen)
            {
                tabIndex = 0;
                GUILayout.BeginHorizontal();
                lineOpen = true;
            }



        }

        public static void end(this GameObject go)
        {
#if UNITY_EDITOR


            //   if (paintingPlayAreaGUI == false)
            ef.end(go);

#endif

        }

        // ############ GUI
        static Countless<string> dependenciesNames = new Countless<string>();
        static bool recordDependencies;
        static bool showDependenciesRecording = false;
        static string dependenciesEncoded = "";
        public static bool RecordDependencies() {

            bool changed = false;

            if ("Dependencies".foldout(ref showDependenciesRecording))  {

                if (recordDependencies) {
                    recordDependencies = false;
                    dependenciesEncoded = dependenciesNames.Encode().ToString();
                }

                if ("Read".Click("Will copy visible dependencies").nl())
                {
                    changed = true;
                    recordDependencies = true;
                }

                changed |= "data".edit(ref dependenciesEncoded);
                if (dependenciesEncoded.Length > 0 && icon.Load.Click())
                {
                    changed = true;
                    dependenciesEncoded.DecodeInto(out dependenciesNames);
                }
            }
            return changed;
        }

        public static void DropFocus()
        {
            FocusControl("_");
        }

        public static string ToPEGIstring(this object obj) {

            if (obj == null) return "NULL";

            var dn = obj as iGotDisplayName;
            if (dn != null)
                return dn.NameForPEGIdisplay();

            var sn = obj as iGotName;
            if (sn != null)
                return sn.NameForPEGI;

            return obj.ToString();
        }

        public static void FocusControl(string name)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.focusTextInControl(name);
            }
            else
#endif
            {
                GUI.FocusControl(name);

            }

        }

        public static void NameNext(string name)
        {
            GUI.SetNextControlName(name);
        }

        public static int NameNextUnique(ref string name)
        {
            name += focusInd.ToString();
            GUI.SetNextControlName(name);
            focusInd++;

            return (focusInd - 1);
        }

        public static string nameFocused
        {
            get
            {
                return GUI.GetNameOfFocusedControl();
            }
        }

        public static bool select(this string text, int width, ref int value, string[] array)
        {
            write(text, width);
            return select(ref value, array);
        }

        public static bool select<T>(this string text, int width, ref T value, List<T> array)
        {
            write(text, width);
            return select(ref value, array);
        }

        public static bool select(this string text, ref string val, List<string> lst)
        {
            write(text);
            return select(ref val, lst);
        }

        public static bool select<T>(this string text, ref T value, List<T> list)
        {
            write(text);
            return select(ref value, list);
        }

        public static bool select(this string text, int width, ref string val, List<string> lst)
        {
            write(text, width);
            return select(ref val, lst);
        }

        public static bool select<T>(this string text, ref int ind, List<T> lst)
        {
            write(text);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, ref int ind, T[] lst)
        {
            write(text);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, int width, ref int ind, T[] lst)
        {
            write(text, width);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, string tip, int width, ref int ind, T[] lst)
        {
            write(text, tip, width);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, ref T value, T[] lst)
        {
            write(text);
            return select(ref value, lst);
        }

        public static bool select<T>(this string text, string tip, ref int ind, List<T> lst)
        {
            write(text, tip);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, int width, ref int ind, List<T> lst)
        {
            write(text, width);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, string tip, int width, ref int ind, List<T> lst)
        {
            write(text, tip, width);
            return select(ref ind, lst);
        }

        public static bool select(ref int no, List<string> from)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, from.ToArray());
            }
            else
#endif

            {
                if ((from == null) || (from.Count == 0)) return false;

                foldout(from[Mathf.Min(from.Count - 1, no)]);
                newLine();

                if (isFoldedOut)
                {
                    for (int i = 0; i < from.Count; i++)
                    {
                        if (i != no) //write("Selected: "+from[i]);
                                     // else
                            if (Click(i + ": " + from[i], 100))
                            {
                                no = i;
                                foldIn();
                                return true;
                            }

                        newLine();
                    }
                }

                GUILayout.Space(10);

                return false;
            }
        }

        public static bool select(ref int no, string[] from)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, from);
            }
            else
#endif

            {
                if ((from == null) || (from.Length == 0)) return false;

                foldout((no > -1) ? from[Mathf.Min(from.Length - 1, no)] : ". . .");
                newLine();

                if (isFoldedOut)
                {
                    for (int i = 0; i < from.Length; i++)
                    {
                        if (i != no) //write("Selected: "+from[i]);
                                     // else
                            if (Click(i + ": " + from[i], 100))
                            {
                                no = i;
                                foldIn();
                                return true;
                            }

                        newLine();
                    }
                }

                GUILayout.Space(10);

                return false;
            }
        }

        public static bool select(ref int no, string[] from, int width)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, from, width);
            }
            else
#endif

            {
                if ((from == null) || (from.Length == 0)) return false;

                foldout(from[Mathf.Min(from.Length - 1, no)]);
                newLine();

                if (isFoldedOut)
                {
                    for (int i = 0; i < from.Length; i++)
                    {
                        if (i != no) //write("Selected: "+from[i]);
                                     // else
                            if (Click(i + ": " + from[i], width))
                            {
                                no = i;
                                foldIn();
                                return true;
                            }

                        newLine();
                    }
                }

                GUILayout.Space(10);

                return false;
            }
        }

        public static bool select<T>(ref T val, T[] lst)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select<T>(ref val, lst);
            }
            else
#endif

            {
                checkLine();

                List<string> lnms = new List<string>();
                List<int> indxs = new List<int>();

                int jindx = -1;

                for (int j = 0; j < lst.Length; j++)
                {
                    T tmp = lst[j];
                    if (!tmp.isGenericNull())
                    {
                        if ((!val.isGenericNull()) && val.Equals(tmp))
                            jindx = lnms.Count;
                        lnms.Add(tmp.ToPEGIstring());
                        indxs.Add(j);
                    }
                }

                if (select(ref jindx, lnms.ToArray()))
                {
                    val = lst[indxs[jindx]];
                    return true;
                }

                return false;
            }
        }

        public static bool select<T>(ref int ind, T[] lst)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select<T>(ref ind, lst);
            }
            else
#endif

            {
                checkLine();

                List<string> lnms = new List<string>();
                List<int> indxs = new List<int>();

                //int jindx = -1;
                int before = ind;
                ind = ind.ClampZeroTo(lst.Length);

                // var val = lst[ind];

                for (int j = 0; j < lst.Length; j++)
                {
                    T tmp = lst[j];
                    if (!tmp.isGenericNull())
                    {
                        lnms.Add(tmp.ToPEGIstring());
                        indxs.Add(j);
                    }
                }

                if (select(ref ind, lnms.ToArray()))
                    return true;

                return before != ind;
            }
        }

        public static bool select<T>(ref T val, List<T> lst)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select<T>(ref val, lst);
            }
            else
#endif

            {
                checkLine();

                List<string> lnms = new List<string>();
                List<int> indxs = new List<int>();

                int jindx = -1;

                for (int j = 0; j < lst.Count; j++)
                {
                    T tmp = lst[j];
                    if (!tmp.isGenericNull())
                    {
                        if ((!val.isGenericNull()) && val.Equals(tmp))
                            jindx = lnms.Count;
                        lnms.Add(tmp.ToPEGIstring());
                        indxs.Add(j);
                    }
                }

                if (select(ref jindx, lnms.ToArray()))
                {
                    val = lst[indxs[jindx]];
                    return true;
                }

                return false;
            }
        }

        public static bool select(ref string val, List<string> lst)
        {

            var ind = -1;

            for (int i = 0; i < lst.Count; i++)
                if (lst[i] != null && lst[i].SameAs(val))
                {
                    ind = i;
                    break;
                }

            if (select(ref ind, lst))
            {
                val = lst[ind];
                return true;
            }

            return false;
        }

        public static bool select<T>(ref int ind, List<T> lst)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select<T>(ref ind, lst);
            }
            else
#endif

            {
                checkLine();

                List<string> lnms = new List<string>();
                List<int> indxs = new List<int>();

                int jindx = -1;

                for (int j = 0; j < lst.Count; j++)
                {
                    if (lst[j] != null)
                    {
                        if (ind == j)
                            jindx = indxs.Count;
                        lnms.Add(lst[j].ToPEGIstring());
                        indxs.Add(j);

                    }
                }

                if (select(ref jindx, lnms.ToArray()))
                {
                    ind = indxs[jindx];
                    return true;
                }

                return false;
            }
        }

        public static bool select<T>(ref int ind, List<T> lst, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref ind, lst, width);
            }
            else
#endif

            {
                checkLine();

                List<string> lnms = new List<string>();
                List<int> indxs = new List<int>();

                int jindx = -1;

                for (int j = 0; j < lst.Count; j++)
                {
                    if (lst[j] != null)
                    {
                        if (ind == j)
                            jindx = indxs.Count;
                        lnms.Add(lst[j].ToPEGIstring());
                        indxs.Add(j);

                    }
                }

                if (select(ref jindx, lnms.ToArray(), width))
                {
                    ind = indxs[jindx];
                    return true;
                }

                return false;
            }
        }

        public static bool select<T>(ref int i, T[] ar, bool clampValue) where T : IeditorDropdown
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select<T>(ref i, ar, clampValue);
            }
            else
#endif

            {
                checkLine();

                bool changed = false;

                List<string> lnms = new List<string>();
                List<int> ints = new List<int>();

                int ind = -1;

                if (clampValue)
                {
                    i = i.ClampZeroTo(ar.Length);
                    if (ar[i].showInDropdown() == false)
                        for (int v = 0; v < ar.Length; v++)
                        {
                            T val = ar[v];
                            if (val.showInDropdown())
                            {
                                i = v;
                                changed = true;
                                break;
                            }
                        }
                }

                for (int j = 0; j < ar.Length; j++)
                {
                    T val = ar[j];
                    if (val.showInDropdown())
                    {
                        if (i == j) ind = ints.Count;
                        lnms.Add(val.ToPEGIstring());
                        ints.Add(j);
                    }
                }

                //int newNo = ind; EditorGUILayout.Popup(ind, lnms.ToArray());

                if (select(ref ind, lnms.ToArray())) // (newNo != ind)
                {
                    i = ints[ind];
                    changed = true;
                }
                return changed;
            }
        }

        public static bool select<T>(ref int no, CountlessSTD<T> tree) where T : iSTD, new()
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, tree);
            }
            else
#endif
            {
                List<int> inds;
                List<T> objs = tree.GetAllObjs(out inds);
                List<string> filtered = new List<string>();
                int tmpindex = -1;
                for (int i = 0; i < objs.Count; i++)
                {
                    if (no == inds[i])
                        tmpindex = i;
                    filtered.Add(objs[i].ToPEGIstring());
                }

                if (select(ref tmpindex, filtered.ToArray()))
                {
                    no = inds[tmpindex];
                    return true;
                }
                return false;
            }
        }

        public static bool select<T>(ref int no, Countless<T> tree)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, tree);
            }
            else
#endif
            {
                List<int> inds;
                List<T> objs = tree.GetAllObjs(out inds);
                List<string> filtered = new List<string>();
                int tmpindex = -1;
                for (int i = 0; i < objs.Count; i++)
                {
                    if (no == inds[i])
                        tmpindex = i;
                    filtered.Add(objs[i].ToPEGIstring());
                }

                if (select(ref tmpindex, filtered.ToArray()))
                {
                    no = inds[tmpindex];
                    return true;
                }
                return false;
            }
        }

        public static bool selectEnum(ref int current, Type type)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref current, type);
            }
            else
#endif

            {
                checkLine();
                int tmpVal = current;

                string[] names = Enum.GetNames(type);
                int[] val = (int[])Enum.GetValues(type);

                for (int i = 0; i < val.Length; i++)
                    if (val[i] == current)
                        tmpVal = i;

                if (select(ref tmpVal, names))
                {
                    current = val[tmpVal];
                    return true;
                }

                return false;
            }
        }

        public static bool select<T>(ref int current)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref current, typeof(T));
            }
            else
#endif

            {
                checkLine();
                int tmpVal = current;
                Type t = typeof(T);
                string[] names = Enum.GetNames(t);
                int[] val = (int[])Enum.GetValues(t);

                for (int i = 0; i < val.Length; i++)
                    if (val[i] == current)
                        tmpVal = i;

                if (select(ref tmpVal, names))
                {
                    current = val[tmpVal];
                    return true;
                }

                return false;
            }
        }

        public static bool select(ref int current, Dictionary<int, string> from)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref current, from);
            }
            else
#endif

            {
                checkLine();

                checkLine();
                string[] options = new string[from.Count];

                int ind = current;

                for (int i = 0; i < from.Count; i++)
                {
                    var e = from.ElementAt(i);
                    options[i] = e.Value;
                    if (current == e.Key)
                        ind = i;
                }

                if (select(ref ind, options))
                {
                    current = from.ElementAt(ind).Key;
                    return true;
                }
                return false;
            }
        }

        public static bool select(ref int current, Dictionary<int, string> from, int width)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref current, from, width);
            }
            else
#endif
        
            {
                checkLine();
                
                string[] options = new string[from.Count];

                int ind = current;

                for (int i = 0; i < from.Count; i++)
                {
                    var e = from.ElementAt(i);
                    options[i] = e.Value;
                    if (current == e.Key)
                        ind = i;
                }

                if (select(ref ind, options, width))
                {
                    current = from.ElementAt(ind).Key;
                    return true;
                }
                return false;
            }
        }

        public static bool select(CountlessInt cint, int ind, Dictionary<int, string> from)
        {

            int value = cint[ind];

            if (select(ref value, from))
            {

                cint[ind] = value;

                return true;
            }
            return false;
        }

        public static bool select(ref int no, Texture[] tex)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, tex);
            }
            else
#endif
            {

                if (tex.Length == 0) return false;

                checkLine();

                List<string> tnames = new List<string>();
                List<int> tnumbers = new List<int>();

                int curno = 0;
                for (int i = 0; i < tex.Length; i++)
                    if (tex[i] != null)
                    {
                        tnumbers.Add(i);
                        tnames.Add(i + ": " + tex[i].name);
                        if (no == i) curno = tnames.Count - 1;
                    }

                bool changed = select(ref curno, tnames.ToArray());

                if (changed)
                {
                    if ((curno >= 0) && (curno < tnames.Count))
                        no = tnumbers[curno];
                }

                return changed;
            }
        }

        public static bool selectOrAdd(ref int no, ref Texture[] tex)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.selectOrAdd(ref no, ref tex);
            }
            else
#endif
            {


                return select(ref no, tex);
            }
        }

        public static bool select_or_edit<T>(ref T obj, List<T> list) where T : UnityEngine.Object
        {
            if (list == null || list.Count == 0)
                return edit(ref obj);
            else return select(ref obj, list);
        }

        public static bool select_or_edit<T>(this string name, ref T obj, List<T> list) where T : UnityEngine.Object
        {
            write(name);
            return select_or_edit(ref obj, list);
        }

        public static bool select_or_edit<T>(this string name, int width, ref T obj, List<T> list) where T : UnityEngine.Object
        {
            write(name, width);
            return select_or_edit(ref obj, list);
        }

        // Foldouts        
        public static bool foldout(this string txt, ref bool state)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.foldout(txt, ref state);
            }
            else
#endif

            {

                checkLine();

                if (Click((state ? "..⏵ " : "..⏷ ") + txt))
                    state = !state;


                isFoldedOut = state;

                return isFoldedOut;
            }
        }

        public static bool foldout(this string txt, ref int selected, int current)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.foldout(txt, ref selected, current);
            }
            else
#endif
            {

                checkLine();

                isFoldedOut = (selected == current);

                if (Click((isFoldedOut ? "..⏵ " : "..⏷ ") + txt))
                {
                    if (isFoldedOut)
                        selected = -1;
                    else
                        selected = current;
                }

                isFoldedOut = selected == current;

                return isFoldedOut;
            }
        }

        public static bool foldout(this Texture2D tex, string text, ref int selected, int current)  {

            if (selected == current)
            {
                if (icon.FoldedOut.Click(text, 30))
                    selected = -1;
            }
            else
            {
                if (tex.Click(text, 25))
                    selected = current;
            }
            return selected == current;
        }

        public static bool foldout(this Texture2D tex, string text, ref bool state)
        {

            if (state)
            {
                if (icon.FoldedOut.Click(text, 30))
                    state = false;
            }
            else
            {
                if (tex.Click(text, 25))
                    state = true;
            }
            return state;
        }

        public static bool foldout(this icon ico, string text, ref bool state)
        {
            return ico.getIcon().foldout(text, ref state);
        }

        public static bool foldout(this icon ico, string text, ref int selected, int current)
        {
            return ico.getIcon().foldout(text, ref selected, current);
        }

        public static bool fold_enter_exit(ref int selected, int current) {

            if (selected == current) {
                if (icon.Exit.Click())
                    selected = -1;
            }
            else if (selected == -1 && icon.Enter.Click())
                selected = current;

            return (selected == current);
        }
        
        public static bool fold_enter_exit(this icon ico, string txt, ref int selected, int current)
        {

            if (selected == current)
            {
                if (icon.Exit.ClickUnfocus(txt))
                    selected = -1;
            }
            else if (selected == -1)
            {
                if (ico.ClickUnfocus(txt))
                    selected = current;
                write(txt);
            }

            return (selected == current);
        }

        public static bool fold_enter_exit(this string txt, ref int selected, int current) {

            return icon.Enter.fold_enter_exit(txt, ref selected, current);

           /* if (selected == current)
            {
                if (icon.Exit.ClickUnfocus(txt))
                    selected = -1;
            }
            else if (selected == -1)
            {
                if (icon.Enter.ClickUnfocus(txt))
                    selected = current;
                write(txt);
            }
            
            return (selected == current);*/
        }
        
        public static bool foldout(this string txt)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.foldout(txt);
            }
            else
#endif

            {

                foldout(txt, ref selectedFold, elementIndex);

                elementIndex++;

                return isFoldedOut;
            }

        }

        public static void foldIn()
        {
            selectedFold = -1;
        }

        public static void Space()
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.Space();
            }
            else
#endif

            {
                checkLine();
                GUILayout.Space(10);
            }
        }

        // Buttons
        const int defaultButtonSize = 25;

        public static int selectedTab;
        public static void ClickTab(ref bool open, string text)
        {
            if (open) write("|" + text + "|", 60);
            else if (Click(text, 40)) selectedTab = tabIndex;
            tabIndex++;
        }

        public static bool ClickUnfocus(this string text, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                if (ef.Click(text, width))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }
            else
#endif

            {
                checkLine();
                if (GUILayout.Button(text, GUILayout.MaxWidth(width)))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }

        }

        public static bool ClickUnfocus(this Texture tex, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                if (ef.Click(tex, width))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }
            else
#endif

            {
                checkLine();
                if (GUILayout.Button(tex, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(width)))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }

        }

        public static bool ClickUnfocus(this Texture tex, string tip, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                if (ef.Click(tex, tip, width))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }
            else
#endif

            {
                return Click(tex, tip, width);
            }

        }


        public static bool ClickUnfocus(this string text)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                if (ef.Click(text))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }
            else
#endif

            {
                checkLine();
                if (GUILayout.Button(text))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }

        }

        public static bool Click(this string text, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(text, width);
            }
            else
#endif

            {
                checkLine();
                return GUILayout.Button(text, GUILayout.MaxWidth(width));
            }

        }

        public static bool Click(this string text)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(text);
            }
            else
#endif

            {
                checkLine();
                return GUILayout.Button(text);
            }

        }

        public static bool Click(this string text, string tip)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(text, tip);
            }
            else
#endif

            {
                checkLine();
                GUIContent cont = new GUIContent();
                cont.text = text;
                cont.tooltip = tip;
                return GUILayout.Button(cont);
            }

        }

        public static bool Click(this string text, string tip, int width)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(text, tip, width);
            }
            else
#endif

            {
                checkLine();
                GUIContent cont = new GUIContent();
                cont.text = text;
                cont.tooltip = tip;
                return GUILayout.Button(cont, GUILayout.MaxWidth(width));
            }

        }

        public static bool Click(this Texture img)
        {
            return img.Click(defaultButtonSize);
            
        }

        public static bool Click(this Texture img, string tip)
        {

            return   img.Click(  tip, defaultButtonSize);

        }

        public static bool Click(this Texture img, int size)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(img, size);
            }
            else
#endif

            {
                checkLine();
                return GUILayout.Button(img, GUILayout.MaxWidth(size+5), GUILayout.MaxHeight(size));
            }

        }

        public static bool Click(this Texture img, string tip, int size)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(img, tip, size);
            }
            else
#endif
            {
                checkLine();
                return GUILayout.Button(new GUIContent(img, tip), GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size));
            }

        }

        public static bool Click(this icon icon)
        {
            return Click(icon.getIcon(), defaultButtonSize);
        }

        public static bool ClickUnfocus(this icon icon)
        {
            return ClickUnfocus(icon.getIcon(), defaultButtonSize);
        }
        
        public static bool ClickUnfocus(this icon icon, string text)
        {
            return ClickUnfocus(icon.getIcon(), defaultButtonSize);
        }
        
        public static bool ClickUnfocus(this icon icon, string text, int width)
        {
            return ClickUnfocus(icon.getIcon(), text, width);
        }

        public static bool ClickUnfocus(this icon icon,  int width)
        {
            return ClickUnfocus(icon.getIcon(), width);
        }

        public static bool Click(this icon icon, int size)
        {
            return Click(icon.getIcon(), size);
        }

        public static bool Click(this icon icon, string tip, int size)
        {
            return Click(icon.getIcon(), tip, size);
        }

        public static bool Click(this icon icon, string tip)
        {
            return Click(icon.getIcon(), tip, defaultButtonSize);
        }
        
        public static bool ClickToEditScript()
        {

#if UNITY_EDITOR
            if (icon.Script.Click("Click to edit current position in a script", 20))
            {

                var frame = new StackFrame(1, true);

                string fileName = frame.GetFileName();

                fileName = fileName.RemoveFirst(fileName.IndexOf("Assets", StringComparison.InvariantCulture));

                UnityEditorInternal.InternalEditorUtility
                                   .OpenFileAtLineExternal(fileName,
                                    frame.GetFileLineNumber()
                                   );
                return true;
            }
#endif

            return false;
        }
        
        public static bool editKey(ref Dictionary<int, string> dic, int key)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.editKey(ref dic, key);
            }
            else
#endif
            {
                checkLine();
                int pre = key;
                if (editDelayed(ref key, 40))
                    return dic.TryChangeKey(pre, key);

                return false;
            }
        }

        public static bool edit(ref Dictionary<int, string> dic, int atKey)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref dic, atKey);
            }
            else
#endif
            {
                string before = dic[atKey];
                if (editDelayed(ref before, 40))
                {
                    dic[atKey] = before;
                    return false;
                }

                return false;
            }
        }
        
        public static bool edit(this string name, ref AnimationCurve val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(name, ref val);
            }
            else
#endif
                return false;
        }

        public static bool edit(ref int current, Type type)
        {
            return selectEnum(ref current, type);
        }

        public static bool edit<T>(ref T field) where T : UnityEngine.Object
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref field);
            }
            else
#endif
            {
                checkLine();
                write(field == null ? "-no " + typeof(T).ToString() : field.ToPEGIstring());
                return false;
            }

        }

        public static bool edit<T>(ref T field, bool allowDrop) where T : UnityEngine.Object
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref field, allowDrop);
            }
            else
#endif
            {
                checkLine();
                write(field == null ? "-no " + typeof(T).ToString() : field.ToPEGIstring());
                return false;
            }

        }

        public static bool edit(GameObject go)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {

                string name = go.name;
                if (ef.edit(ref name))
                {
                    go.name = name;
                    return true;
                }
                return false;
            }
            else
#endif
            {

                string name = go.name;
                if (edit(ref name))
                {
                    go.name = name;
                    return true;
                }
                return false;
            }

        }

        public static bool edit(ref Vector4 val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= "X".edit(ref val.x) | "Y".edit(ref val.y) | "Z".edit(ref val.z) | "W".edit(ref val.w);
                return modified;
            }
        }

        public static bool edit(this string label, ref Vector4 val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(label, ref val);
            }
            else
#endif
            {

                write(label);
                bool modified = false;
                modified |= edit(ref val.x);
                modified |= edit(ref val.y);
                modified |= edit(ref val.z);
                modified |= edit(ref val.w);
                return modified;
            }


        }

        public static bool edit(ref Vector3 val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= "X".edit(ref val.x) | "Y".edit(ref val.y) | "Z".edit(ref val.z);
                return modified;
            }
        }

        public static bool edit(ref Vector2 val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= edit(ref val.x);
                modified |= edit(ref val.y);
                return modified;
            }


        }

        public static bool edit(this string label, ref Vector2 val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(label, ref val);
            }
            else
#endif
            {

                write(label);
                bool modified = false;
                modified |= edit(ref val.x);
                modified |= edit(ref val.y);
                return modified;
            }


        }

        public static bool edit(ref myIntVec2 val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= edit(ref val.x);
                modified |= edit(ref val.y);
                return modified;
            }


        }

        public static bool edit(ref myIntVec2 val, int min, int max)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val, min, max);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= edit(ref val.x, min, max);
                modified |= edit(ref val.y, min, max);
                return modified;
            }


        }

        public static bool edit(ref myIntVec2 val, int min, myIntVec2 max)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val, min, max);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= edit(ref val.x, min, max.x);
                modified |= edit(ref val.y, min, max.y);
                return modified;
            }


        }

        public static bool edit(ref linearColor col)
        {
            Color c = col.ToGamma();
            if (edit(ref c))
            {
                col.From(c);
                return true;
            }
            return false;
        }

        public static bool edit(ref Color col)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref col);
            }
            else
#endif
            {
                checkLine();
                // Color editing not implemented yet
                return false;
            }
        }

        public static bool edit(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val, min, max);
            }
            else
#endif
            {
                checkLine();
                float before = val;

                val = GUILayout.HorizontalSlider(before, min, max);
                return (before != val);
            }
        }

        public static bool edit(ref int val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before);
                if (String.Compare(before, newval) != 0)
                {

                    int newValue;
                    bool parsed = int.TryParse(newval, out newValue);
                    if (parsed)
                        val = newValue;

                    return true;
                }
                return false;
            }

        }

        public static bool edit(ref int val, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val, width);
            }
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
                if (String.Compare(before, newval) != 0)
                {

                    int newValue;
                    bool parsed = int.TryParse(newval, out newValue);
                    if (parsed)
                        val = newValue;

                    return true;
                }
                return false;
            }
        }

        public static bool edit(ref float val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before);
                if (String.Compare(before, newval) != 0)
                {

                    float newValue;
                    bool parsed = float.TryParse(newval, out newValue);
                    if (parsed)
                        val = newValue;

                    return true;
                }
                return false;
            }
        }

        public static bool edit(this string label, ref float val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(label, ref val);
            }
            else
#endif
            {
                write(label);
                return edit(ref val);
            }
        }

        public static bool edit(ref int val, int min, int max)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val, (int)min, (int)max);
            }
            else
#endif
            {
                checkLine();
                float before = val;
                //if (edit(ref val))
                //val = Mathf.Clamp(val, min, max);
                val = (int)GUILayout.HorizontalSlider(before, min, max);
                return (before != val);
            }
        }

        public static bool editPOW(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.editPOW(ref val, min, max);
            }
            else
#endif
            {
                checkLine();
                float before = Mathf.Sqrt(val);
                float after = GUILayout.HorizontalSlider(before, min, max);
                if (before != after)
                {
                    val = after * after;
                    return true;
                }
                return false;

            }
        }

        public static bool edit(this Sentance val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(val);
            }
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                if (edit(ref before))
                {
                    val.setTranslation(before);
                    return true;
                }
                return false;
            }
        }

        public static bool editEnum<T>(this string text, string tip, int width, ref T eval)
        {
            write(text, tip, width);
            return editEnum<T>(ref eval);
        }

        public static bool editEnum<T>(this string text, int width, ref T eval)
        {
            write(text, width);
            return editEnum<T>(ref eval);
        }

        public static bool editEnum<T>(this string text, ref T eval) {
            write(text);
            return editEnum<T>(ref eval);
        }

        public static bool editEnum<T>(ref T eval) {

            int val = Convert.ToInt32(eval);

            if (selectEnum(ref val, typeof(T)))
            {

                eval = (T)((object)val);
                return true;
            }

            return false;
        }

        public static bool editTexture(this Material mat, string name)
        {
            return mat.editTexture(name, name);
        }

        public static bool editTexture(this Material mat, string name, string display)
        {
            write(display);
            Texture tex = mat.GetTexture(name);

            if (edit(ref tex))
            {
                mat.SetTexture(name, tex);
                return true;
            }

            return false;
        }

        static string editedText;
        static string editedHash = "";
        public static bool editDelayed(ref string val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.editDelayed(ref val);
            }
            else
#endif
            {

                checkLine();

                if ((KeyCode.Return.isDown() && (val.GetHashCode().ToString() == editedHash)))
                {
                    GUILayout.TextField(val);


                    val = editedText;

                    return true;
                }

                string tmp = val;
                if (edit(ref tmp))
                {
                    editedText = tmp;
                    editedHash = val.GetHashCode().ToString();
                }

                return false;
            }
        }

        public static bool editDelayed(ref string val, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.editDelayed(ref val, width);
            }
            else
#endif
            {

                checkLine();

                if ((KeyCode.Return.isDown() && (val.GetHashCode().ToString() == editedHash)))
                {
                    GUILayout.TextField(val);


                    val = editedText;

                    return true;
                }

                string tmp = val;
                if (edit(ref tmp, width))
                {
                    editedText = tmp;
                    editedHash = val.GetHashCode().ToString();
                }

                return false;
            }
        }


        public static bool editDelayed(this string txt, ref int val, int width)
        {
            write(txt);
            return editDelayed(ref val, width);
        }

        public static bool editDelayed(this string txt, int width, ref string val)
        {
            write(txt, width);
            return editDelayed(ref val);
        }

        static int editedInteger;
        static int editedIntegerIndex;
        public static bool editDelayed(ref int val, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.editDelayed(ref val, width);
            }
            else
#endif
            {

                checkLine();

                int tmp = val;

                if (KeyCode.Return.isDown() && (elementIndex == editedIntegerIndex))
                {
                    edit(ref tmp);
                    val = editedInteger;
                    elementIndex++;
                    return true;
                }


                if (edit(ref tmp))
                {
                    editedInteger = tmp;
                    editedIntegerIndex = elementIndex;
                }

                elementIndex++;

                return false;
            }
        }


        public static bool edit(ref string val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                string before = val;
                string newval = GUILayout.TextField(before);
                if (String.Compare(before, newval) != 0)
                {
                    //val = newval;
                    return true;
                }
                return false;
            }
        }

        public static bool edit(ref string val, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val, width);
            }
            else
#endif
            {
                checkLine();
                string before = val;
                string newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
                if (String.Compare(before, newval) != 0)
                {
                    //val = newval;
                    return true;
                }
                return false;
            }
        }

        public static bool editBig(ref string val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.editBig(ref val);
            }
            else
#endif
            {
                checkLine();
                string before = val;
                string newval = GUILayout.TextArea(before);
                if (String.Compare(before, newval) != 0)
                {
                    val = newval;
                    return true;
                }
                return false;
            }
        }

        public static bool editBig(this string name, ref string val)  {

            write(name);
            nl();
            return editBig(ref val);
            
        }

        public static bool edit<T>(ref int current)
        {
            return selectEnum(ref current, typeof(T));
        }

        public static bool edit(this string label, ref linearColor col)
        {
            write(label);
            return edit(ref col);
        }
        
        public static bool edit<T>(this string label, ref T field, int index) where T : UnityEngine.Object {

            return label.edit(ref field, index, dependenciesNames);
        }

        public static bool edit<T>(this string label, ref T field, int index, Countless<string> names) where T : UnityEngine.Object  {

            bool changed = false;

            if (recordDependencies && field != null)
                names[index] = field.name;

            if (field == null && names[index] != null) {
                changed |= names[index].edit(70, ref field);
                if (icon.Search.Click().nl()) {
                    UnityEngine.Debug.Log("Searching for ...");
                    changed = true;
                }
            }
            else
                changed |= label.edit(70, ref field);

            if (changed && field != null)
                names[index] = field.name;

            return changed;
        }

        public static bool edit<T>(this string label, ref T field) where T : UnityEngine.Object
        {
            write(label);
            return edit(ref field);
        }

        public static bool edit<T>(this string label, ref T field, bool allowDrop) where T : UnityEngine.Object
        {
            write(label);
            return edit(ref field, allowDrop);
        }

        public static bool edit<T>(this string label, int width, ref T field) where T : UnityEngine.Object
        {
            write(label, width);
            return edit(ref field);
        }

        public static bool edit<T>(this string label, int width, ref T field, bool allowDrop) where T : UnityEngine.Object
        {
            write(label, width);
            return edit(ref field, allowDrop);
        }

        public static bool edit<T>(this string label, string tip, int width, ref T field) where T : UnityEngine.Object
        {
            write(label, tip, width);
            return edit(ref field);
        }

        public static bool edit<T>(this string label, string tip, int width, ref T field, bool allowDrop) where T : UnityEngine.Object
        {
            write(label, tip, width);
            return edit(ref field, allowDrop);
        }

        public static bool edit(this string label, ref myIntVec2 val)
        {
            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref myIntVec2 val)
        {
            write(label, width);
            nl();
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref myIntVec2 val, int min, int max)
        {
            write(label, width);
            nl();
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref myIntVec2 val, int min, myIntVec2 max)
        {
            write(label, width);
            nl();
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, ref Vector3 val)
        {
            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(int ind, CountlessInt val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                int before = val[ind];
                if (ef.edit(ref before, 45))
                {
                    val[ind] = before;
                    return true;
                }
                return false;
            }
            else
#endif
            {
                int before = val[ind];
                if (edit(ref before, 45))
                {
                    val[ind] = before;
                    return true;
                }
                return false;
            }
        }

        public static bool edit(this string label, ref int val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref float val)
        {
            write(label, width);
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref float from, ref float to)
        {
            write(label, width);
            bool changed = false;
            if (edit(ref from))
            {
                changed = true;
                to = Mathf.Max(from, to);
            }

            write("-", 10);

            if (edit(ref to))
            {
                from = Mathf.Min(from, to);
                changed = true;
            }

            return changed;
        }

        public static bool edit(this string label, ref float val, float min, float max)
        {
            write(label);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, ref int val, int min, int max)
        {
            write(label);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref int val, int min, int max)
        {
            write(label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref int val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref int val, int min, int max)
        {
            write(label, tip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref float val, float min, float max)
        {
            write(label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref float val, float min, float max)
        {
            write(label, tip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref int val)
        {
            write(label, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref float val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, ref string val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, ref Color col)
        {
            write(label);
            return edit(ref col);
        }

        public static bool edit(this string label, int width, ref Color col)
        {
            write(label, width);
            return edit(ref col);
        }

        public static bool edit(this string label, string tip, int width, ref Color col)
        {
            write(label, tip, width);
            return edit(ref col);
        }

        public static bool edit(this string label, int width, ref Vector2 v2)
        {
            write(label, width);
            return edit(ref v2);
        }

        public static bool edit(this string label, string tip, int width, ref Vector2 v2)
        {
            write(label, tip, width);
            return edit(ref v2);
        }

        public static bool edit(this string label, int width, ref Vector3 v3)
        {
            write(label, width);
            return edit(ref v3);
        }

        public static bool edit(this string label, string tip, int width, ref Vector3 v3)
        {
            write(label, tip, width);
            return edit(ref v3);
        }

        public static bool edit(this string label, int width, ref string val)
        {
            write(label, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref string val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static int editEnum<T>(T val)
        {
            int ival = Convert.ToInt32(val);
            selectEnum(ref ival, typeof(T));
            return ival;
        }

        public static bool toggleInt(ref int val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.toggleInt(ref val);
            }
            else
#endif
            {
                checkLine();
                bool before = val > 0;
                if (pegi.toggle(ref before))
                {
                    val = before ? 1 : 0;
                    return true;
                }
                return false;
            }
        }

        public static bool toggle(ref bool val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.toggle(ref val);
            }
            else
#endif
            {
                checkLine();
                bool before = val;
                val = GUILayout.Toggle(val, "", GUILayout.MaxWidth(30));
                return (before != val);
            }
        }

        public static bool toggle(ref bool val, string text, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(text, width);
                return ef.toggle(ref val);
            }
            else
#endif
            {
                checkLine();
                bool before = val;
                val = GUILayout.Toggle(val, text);
                return (before != val);
            }
        }

        public static bool toggle(ref bool val, string text, string tip)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.toggle(ref val, text, tip);
            }
            else
#endif
            {
                checkLine();
                bool before = val;
                GUIContent cont = new GUIContent();
                cont.text = text;
                cont.tooltip = tip;
                val = GUILayout.Toggle(val, cont);
                return (before != val);
            }
        }

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width)
        {
            return toggle(ref val, TrueIcon.getIcon(), FalseIcon.getIcon(), tip, width);
        }

        public static bool toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.toggle(ref val, TrueIcon, FalseIcon, tip, width);
            }
            else
#endif
            {
                checkLine();
                bool before = val;

                if (val)
                {
                    if (Click(TrueIcon, tip, width))
                        val = false;
                }
                else
                {
                    if (Click(FalseIcon, tip, width))
                        val = true;
                }



                return (before != val);
            }


        }

        public static bool toggle(ref bool val, string text, string tip, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(text, tip, width);
                return ef.toggle(ref val);
            }
            else
#endif
            {
                checkLine();
                bool before = val;
                GUIContent cont = new GUIContent();
                cont.text = text;
                cont.tooltip = tip;
                val = GUILayout.Toggle(val, cont);
                return (before != val);
            }
        }

        public static bool toggle(int ind, CountlessBool tb)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.toggle(ind, tb);
            }
            else
#endif
            {
                bool has = tb[ind];
                if (toggle(ref has))
                {
                    tb.Toggle(ind);
                    return true;
                }
                return false;
            }
        }

        public static bool toggle(this icon img, ref bool val)
        {
            write(img.getIcon(), 25);
            return toggle(ref val);
        }

        public static bool toggle(this Texture img, ref bool val)
        {
            write(img, 25);
            return toggle(ref val);
        }

        public static bool toggleInt(this string text, ref int val)
        {
            write(text);
            return toggleInt(ref val);
        }

        public static bool toggleInt(this string text, string hint, ref int val)
        {
            write(text, hint);
            return toggleInt(ref val);
        }

        public static bool toggle(this string text, ref bool val)
        {
            write(text);
            return toggle(ref val);
        }

        public static bool toggle(this string text, int width, ref bool val)
        {
            write(text, width);
            return toggle(ref val);
        }

        public static bool toggle(this string text, string tip, ref bool val)
        {
            write(text, tip);
            return toggle(ref val);
        }

        public static bool toggle(this string text, string tip, int width, ref bool val)
        {
            write(text, tip, width);
            return toggle(ref val);
        }

        public static void write<T>(T field) where T : UnityEngine.Object
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(field);
            }
            else
#endif
            {
                checkLine();
                write(field == null ? "-no " + typeof(T).ToString() : field.ToPEGIstring());
            }

        }

        public static void write<T>(this string label, string tip, int width, T field) where T : UnityEngine.Object
        {
            write(label, tip, width);
            write(field);

        }

        public static void write<T>(this string label, int width, T field) where T : UnityEngine.Object
        {
            write(label, width);
            write(field);

        }

        public static void write<T>(this string label, T field) where T : UnityEngine.Object
        {
            write(label);
            write(field);

        }

        public static void write(Texture img, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(img, width);
            }
            else
#endif
            {
                checkLine();
                GUIContent c = new GUIContent();
                c.image = img;

                GUILayout.Label(c, GUILayout.MaxWidth(width));
            }

        }

        public static void write(this icon icon, int size)
        {
            pegi.write(icon.getIcon(), size);
        }

        public static void write(this icon icon, string tip, int size)
        {
            pegi.write(icon.getIcon(), size);
        }

        public static void write(this string text)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.write(text);
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text);
            }

        }

        public static void write(this string text, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(text, width);
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text, GUILayout.MaxWidth(width));
            }

        }

        public static void write(this string text, string tip)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(text, tip);
            }
            else
#endif
            {
                checkLine();
                GUIContent cont = new GUIContent();
                cont.text = text;
                cont.tooltip = tip;
                GUILayout.Label(cont);
            }

        }

        public static void write(this string text, string tip, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(text, tip, width);
            }
            else
#endif
            {
                checkLine();
                GUIContent cont = new GUIContent();
                cont.text = text;
                cont.tooltip = tip;
                GUILayout.Label(cont, GUILayout.MaxWidth(width));
            }

        }

        public static void writeWarning(this string text)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.writeHint(text, MessageType.Warning);
                ef.newLine();
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text);
                newLine();
            }
        }

        public static void writeHint(this string text)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.writeHint(text, MessageType.Info);
                ef.newLine();
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text);

                pegi.newLine();
            }

        }

        public static void showNotification(this string text)
        {
#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                var lst = Resources.FindObjectsOfTypeAll<SceneView>();
                if (lst.Length > 0)
                    lst[0].ShowNotification(new GUIContent(text));

            }
            else

            {


                //   EditorWindow gameview =
                var ed = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                if (ed != null)
                    ed.ShowNotification(new GUIContent(text));
                //var lst = Resources.FindObjectsOfTypeAll<>();
            }
#endif
        }

        public static void resetOneTimeHint(this string name)
        {
            PlayerPrefs.SetInt(name, 0);
        }

        public static bool writeOneTimeHint(this string text, string name)
        {

            if (PlayerPrefs.GetInt(name) != 0) return false;

            pegi.newLine();

            text += " (press OK)";

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.writeHint(text, MessageType.Info);
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text);
            }

            if (Click("OK", 50).nl())
            {
                PlayerPrefs.SetInt(name, 1);

                return true;
            }


            return false;
        }

        public static bool GetDefine(this string define)
        {

#if UNITY_EDITOR
            return ef.GetDefine(define);
#else
        return true;
#endif
        }

        public static void SetDefine(this string val, bool to)
        {

#if UNITY_EDITOR

            ef.SetDefine(val, to);
#endif
        }

        public static bool edit<T>(this string label, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
        {
            return edit<T>(label, null, null, -1, memberExpression, obj);
        }

        public static bool edit<T>(this string label, string tip, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
        {
            return edit<T>(label, null, tip, -1, memberExpression, obj);
        }

        public static bool edit<T>(this Texture tex, string tip, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
        {
            return edit<T>(null, tex, tip, -1, memberExpression, obj);
        }

        public static bool edit<T>(this string label, Expression<Func<T>> memberExpression)
        {
            return edit<T>(label, null, null, -1, memberExpression, null);
        }

        public static bool edit<T>(this string label, string tip, Expression<Func<T>> memberExpression)
        {
            return edit<T>(label, null, tip, -1, memberExpression, null);
        }

        public static bool edit<T>(this Texture tex, string tip, Expression<Func<T>> memberExpression)
        {
            return edit<T>(null, tex, tip, -1, memberExpression, null);
        }

        public static bool edit<T>(this string label, Texture image, string tip, int width, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
        {

            bool changes = false;
#if UNITY_EDITOR

            SerializedObject sobj = obj == null ? ef.serObj : new SerializedObject(obj); // ScriptableObject.CreateInstance<T>();

            if (paintingPlayAreaGUI == false)
            {
 
                MemberInfo member = ((MemberExpression)memberExpression.Body).Member;
                string name = member.Name;

                var cont = new GUIContent(label, image, tip);

                SerializedProperty tps = sobj.FindProperty(name);
                EditorGUI.BeginChangeCheck();
                if (width == -1)
                    EditorGUILayout.PropertyField(tps, cont, true);
                else
                    EditorGUILayout.PropertyField(tps, cont, true, GUILayout.MaxWidth(width));

                if (EditorGUI.EndChangeCheck())
                {
                    sobj.ApplyModifiedProperties();
                    changes = true;
                }

                /*var ts = new TypeSwitch()
                .Case((int x) => Console.WriteLine("int"))
                .Case((bool x) => Console.WriteLine("bool"))
                .Case((string x) => Console.WriteLine("string"));


                ts.Switch(typeof(T), name);*/




            }
#endif
            return changes;
        }

        public static bool edit<G, T>(this Dictionary<G, T> dic, ref int edited, bool allowDelete)
        {
            bool changed = false;

            int before = edited;
            edited = Mathf.Clamp(edited, -1, dic.Count - 1);
            changed |= (edited != before);

            if (edited == -1)
            {
                for (int i = 0; i < dic.Count; i++)
                {
                    var item = dic.ElementAt(i);
                    var itemKey = item.Key;
                    if (allowDelete && icon.Delete.Click(25))
                    {
                        dic.Remove(itemKey);
                        changed = true;
                        i--;
                    }
                    else
                    {
                        if (item.Name_ClickInspect_PEGI())
                            edited = i;

                    }

                    newLine();
                }

            }
            else
            {
                if (icon.Back.Click(25).nl())
                {
                    changed = true;
                    edited = -1;
                }
                else
                {
                    var std = dic.ElementAt(edited).Value as iPEGI;
                    if (std != null) changed |= std.PEGI();
                }
            }

            pegi.newLine();
            return changed;
        }

        //Lists ...... of Monobehaviour
        public static bool edit_List<T>(this string label, List<T> list, ref int edited, bool allowDelete, ref T added, Countless<string> names) where T : MonoBehaviour
        {
            write(label);
            return list.edit_List(ref edited, allowDelete, ref added, names);
        }

        public static bool edit_List<T>(this List<T> list, ref int edited, bool allowDelete, ref T added, Countless<string> names) where T : MonoBehaviour
        {
            bool changed = false;

            added = default(T);

            int before = edited;
            edited = Mathf.Clamp(edited, -1, list.Count - 1);
            changed |= (edited != before);

            if (edited == -1)
            {

                if (icon.Add.ClickUnfocus().nl())
                {
                    changed = true;
                    list.Add(default(T));
                }

                for (int i = 0; i < list.Count; i++)
                {
                    if (allowDelete && icon.Delete.ClickUnfocus(25))
                    {
                        list.RemoveAt(i);
                        changed = true;
                        i--;
                    }
                    else
                    {
                        var el = list[i];
                        if (el == null)
                        {
                            T obj = null;
                            if (i.ToString().edit(ref obj, i, names))
                            {
                                if (obj)
                                {
                                    list[i] = obj.GetComponent<T>();
                                    if (list[i] == null) (typeof(T).ToString() + " Component not found").showNotification();
                                }
                            }
                        }
                        else
                        {
                            if (list[i].Name_ClickInspect_PEGI())
                                edited = i;
                        }
                    }
                    newLine();
                }


            }
            else
            {
                if (icon.Back.ClickUnfocus().nl())
                {
                    changed = true;
                    edited = -1;
                }
                else
                {
                    var std = list[edited] as iPEGI;
                    if (std != null) changed |= std.PEGI();
                }
            }

            newLine();

            return changed;
        }

        // ...... of Scriptable Object

        public static T edit_List_SO<T>(this string label, List<T> list, ref int edited, ref bool changed) where T : ScriptableObject
        {
            write(label);

            return list.edit_List_SO(ref edited, ref changed);
        }

        public static T edit_List_SO<T>(this List<T> list, ref int edited, ref bool changed) where T : ScriptableObject
        {
            T added = default(T);

            int before = edited;
            edited = Mathf.Clamp(edited, -1, list.Count - 1);
            changed |= (edited != before);
            
            if (edited == -1)
            {
                if (icon.Add.Click().nl())
                    list.Add(null);

                for (int i = 0; i < list.Count; i++)
                {
                    if (icon.Delete.ClickUnfocus(25))
                    {
                        list.RemoveAt(i);
                        changed = true;
                        i--;
                    }
                    else
                    {
                        var el = list[i];
                        if (el == null)
                        {
                            if (edit(ref el))
                                list[i] = el;

                        }
                        else
                        {

                            var named = el as iGotName;
                            if (named != null)
                            {
                                var n = named.NameForPEGI;
                                if (editDelayed(ref n, 120))
                                {
                                  
                                    el.RenameAsset(n);
                                    named.NameForPEGI = n;
                                }
                            }
                            else
                                write(el.ToPEGIstring(), 120);

                            if ((el is iPEGI) && icon.Enter.ClickUnfocus(25))
                                edited = i;


#if UNITY_EDITOR
                            var path = AssetDatabase.GetAssetPath(el);

                            if (path != null && path.Length > 0)
                            {
                                if (icon.Search.Click())
                                    EditorGUIUtility.PingObject(list[i]); // Selection.activeObject = list[i];

                                if (icon.Copy.Click())
                                {

                                    added = el.DuplicateScriptableObject();

                                    list.Insert(i + 1, added);

                                    var indx = added as iGotIndex;

                                    if (indx != null)
                                    {
                                        int max = 0;

                                        foreach (var eee in list)
                                            if (eee != null)
                                            {
                                                var eeind = eee as iGotIndex;
                                                if (eeind != null)
                                                    max = Math.Max(max, eeind.GetIndex() + 1);
                                            }

                                        indx.SetIndex(max);
                                    }

                                }
                            }
#endif
                            }
                        

                    }

                    pegi.newLine();
                }


            }
            else
            {
                if (icon.Exit.ClickUnfocus().nl())
                {
                    changed = true;
                    edited = -1;
                }
                else
                {
                    var std = list[edited] as iPEGI;
                    if (std != null) changed |= std.PEGI();
                }
            }

            pegi.newLine();
            return added;
        }

        public static bool edit_List_SO<T>(this List<T> list, ref int edited) where T : ScriptableObject
        {
            bool changed = false;

            list.edit_List_SO<T>(ref edited, ref changed);

            return changed;
        }

        public static bool edit_List_SO<T>(this string label, List<T> list, ref int edited) where T : ScriptableObject
        {
            write(label);

            bool changed = false;

            list.edit_List_SO<T>(ref edited, ref changed);

            return changed;
        }

        // ...... of Object
        public static bool Name_ClickInspect_PEGI(this System.Object el)
        {

            var named = el as iGotName;
            if (named != null)
            {
                var n = named.NameForPEGI;
                if (edit(ref n,120))
                {
                    named.NameForPEGI = n;
                }
            }
            else
                write(el.ToPEGIstring(), 120);

            if ((el is iPEGI) && icon.Enter.ClickUnfocus(25))
                return true;

            return false;
        }

        static bool clickHighlight (this UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (obj != null && icon.Search.Click())
            {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }
       
        public static bool edit_or_select_List_Obj<T>(this List<T> list, List<T> from, ref int edited, bool allowDelete) where T : UnityEngine.Object
        {

            bool changed = false;

            int before = edited;
            edited = Mathf.Clamp(edited, -1, list.Count - 1);
            changed |= (edited != before);

            if (edited == -1)
            {
                changed |= list.ListAddClick<T>();

                for (int i = 0; i < list.Count; i++)
                {
                    if (allowDelete && icon.Delete.ClickUnfocus(25))
                    {
                        list.RemoveAt(i);
                        changed = true;
                        i--;
                    }
                    else
                    {
                        var el = list[i];
                        if (el == null)
                        {

                            if (from != null && from.Count > 0)
                            {
                                if (pegi.select(ref el, from))
                                    list[i] = el;
                            }


                            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
                            {
                                GameObject obj = null;
                                if (edit(ref obj))
                                {
                                    list[i] = obj.GetComponent<T>();
                                    if (list[i] == null) (typeof(T).ToString() + " Component not found").showNotification();
                                }
                            }
                            else
                            {
                                UnityEngine.Object so = null;
                                if (edit(ref so))
                                    list[i] = so as T;

                            }

                        }
                        else
                        {

                            if (list[i].Name_ClickInspect_PEGI())
                                edited = i;

                            list[i].clickHighlight();
                        }

                    }

                    pegi.newLine();
                }


            }
            else
            {
                if (icon.Exit.ClickUnfocus().nl())
                {
                    changed = true;
                    edited = -1;
                }
                else
                {
                    var std = list[edited] as iPEGI;
                    if (std != null) changed |= std.PEGI();
                }
            }

            pegi.newLine();
            return changed;

        }

        public static bool edit_List_Obj<T>(this List<T> list, bool allowDelete) where T : UnityEngine.Object
        {
            int edited = -1;
            return list.edit_or_select_List_Obj(null, ref edited, allowDelete);

         /*   bool changed = false;

            if (icon.Add.ClickUnfocus().nl())
            {
                changed = true;
                list.Add(null);
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (allowDelete && icon.Delete.ClickUnfocus(25))
                {
                    list.RemoveAt(i);
                    changed = true;
                    i--;
                }
                else
                {

                    var el = list[i];
                    if (el != null)
                    {
                        var n = el.name;
                        if (edit(ref n))
                        {
                            changed = true;
                            el.name = n;
                        }
                    }

                    if (edit(ref el))
                        list[i] = el;

                    el.clickHighlight().nl();

                }
            }
            
            pegi.newLine();
            return changed;*/
        }
        
        public static bool edit_List_Obj<T>(this string label, List<T> list, bool allowDelete) where T : UnityEngine.Object
        {
            write(label);
            return (list.edit_List_Obj(allowDelete));
        }
        
        public static bool edit_List_Obj<T>(this string label, List<T> list, ref int edited) where T : UnityEngine.Object
        {
            write(label);
            return list.edit_List_Obj(ref edited);
        }

        public static bool edit_List_Obj<T>(this List<T> list, ref int edited) where T : UnityEngine.Object
        {
            return list.edit_or_select_List_Obj(null, ref edited, true);
        }

        // ...... of New()
        static bool ListAddClick<T>(this List<T> list, ref T added) where T:new()
        {
            if (icon.Add.ClickUnfocus().nl())
            {
                if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)) || typeof(T).IsSubclassOf(typeof(ScriptableObject)))
                {
                    list.Add(default(T));
                }
                else
                    added = list.AddWithUniqueName();

                return true;
            }

            return false;
        }

        static bool ListAddClick<T>(this List<T> list)
        {
            if (icon.Add.ClickUnfocus().nl()) {
                    list.Add(default(T));
                return true;
            }
            return false;
        }

        public static bool edit_List<T>(this string label, List<T> list, ref int edited, bool allowDelete) where T : new()
        {
            write(label);
            return list.edit_List(ref edited, allowDelete);
        }

        public static bool edit_List<T>(this List<T> list, ref int edited, bool allowDelete) where T : new() {
            bool changes = false;
                list.edit_List(ref edited, allowDelete, ref changes);
            return changes;
        }

        public static T edit_List<T>(this string label, List<T> list, ref int edited, bool allowDelete, ref bool changed) where T : new()
        {
            write(label);
            return list.edit_List(ref edited, allowDelete, ref changed);
        }

        public static T edit_List<T>(this List<T> list, ref int edited, bool allowDelete, ref bool changed) where T : new() {
            T added = default(T);

            int before = edited;
            edited = Mathf.Clamp(edited, -1, list.Count - 1);
            changed |= (edited != before);
            
            if (edited == -1)
            {
                changed |= list.ListAddClick<T>(ref added);
                
                for (int i = 0; i < list.Count; i++)
                {
                    if (allowDelete && icon.Delete.ClickUnfocus(25))
                    {
                        list.RemoveAt(i);
                        changed = true;
                        i--;
                    }
                    else
                    {
                        var el = list[i];
                        if (el == null)
                        {
                            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
                            {
                                GameObject obj = null;
                                if (edit(ref obj))
                                {
                                    list[i] = obj.GetComponent<T>();
                                    if (list[i] == null) (typeof(T).ToString() + " Component not found").showNotification();
                                }
                            }
                        }
                        else
                        {

                            if (list[i].Name_ClickInspect_PEGI())
                                edited = i;

                            (list[i] as UnityEngine.Object).clickHighlight();

                        }

                    }

                    pegi.newLine();
                }

               
            }
            else
            {
                if (icon.Exit.ClickUnfocus().nl())
                {
                    changed = true;
                    edited = -1;
                }
                else
                {
                    var std = list[edited] as iPEGI;
                    if (std != null) changed |= std.PEGI();
                }
            }

            pegi.newLine();
            return added;
        }
        
        // Transform
        static bool _editLocalSpace = false;
        public static bool PEGI_CopyPaste(this Transform tf, ref bool editLocalSpace)
        {
            bool changed = false;

            changed |= "Local".toggle(40, ref editLocalSpace);

            if (icon.Copy.Click("Copy Transform"))
            {
                STDExtensions.copyBufferValue  = tf.Encode(editLocalSpace).ToString();
                changed = true;
            }

            if (STDExtensions.copyBufferValue != null && icon.Paste.Click("Paste Transform"))
            {
                STDExtensions.copyBufferValue.DecodeInto(tf);
                STDExtensions.copyBufferValue = null;
                changed = true;
            }

            pegi.nl();

            return changed;
        }
        public static bool PEGI_CopyPaste(this Transform tf)
        {

            return tf.PEGI_CopyPaste(ref _editLocalSpace);
        }
        
        public static bool PEGI(this Transform tf, bool editLocalSpace)
        {
            bool changed = false;

            if (editLocalSpace)
            {
                var v3 = tf.localPosition;
                if ("Pos".edit(ref v3).nl())
                    tf.localPosition = v3;

                var rot = tf.localRotation.eulerAngles;
                if ("Rot".edit(ref rot).nl())
                    tf.localRotation = Quaternion.Euler(rot);

                v3 = tf.localScale;
                if ("Size".edit(ref v3).nl())
                    tf.localScale = v3;
            }
            else
            {
                var v3 = tf.position;
                if ("Pos".edit(ref v3).nl())
                    tf.position = v3;

                var rot = tf.rotation.eulerAngles;
                if ("Rot".edit(ref rot).nl())
                    tf.rotation = Quaternion.Euler(rot);

            }
            return changed;
        }
        public static bool PEGI(this Transform tf)
        {
            return tf.PEGI(_editLocalSpace);
        }

        public static bool PEGI_Name(this iGotName obj) {
            var n = obj.NameForPEGI;
            if ("Name:".edit(40, ref n))
            {
                obj.NameForPEGI = n;
                return true;
            }
            return false;
        }

        public static bool select_or_Edit_PEGI(this Dictionary<int, string> dic, ref int selected)
        {
            bool changed = false;

            if (editedDic != dic)
            {
                changed |= pegi.select(ref selected, dic);
                if (icon.Add.Click(20))
                {
                    editedDic = dic;
                    changed = true;
                    SetNewKeyToMax(dic);
                }
            }
            else
            {
                if (icon.Close.Click(20)) { editedDic = null; changed = true; }
                else
                    changed |= dic.newElement_PEGI();

            }

            return changed;
        }
        
        public static Dictionary<int, string> editedDic;

        public static string newEnumName = "UNNAMED";
        public static int newEnumKey = 1;
        public static bool edit_PEGI(this Dictionary<int, string> dic)
        {

            bool changed = false;

            pegi.newLine();

            for (int i = 0; i < dic.Count; i++)
            {

                var e = dic.ElementAt(i);
                if (icon.Delete.Click(20))
                    changed |= dic.Remove(e.Key);

                else
                {
                    changed |= pegi.editKey(ref dic, e.Key);
                    if (!changed)
                        changed |= pegi.edit(ref dic, e.Key);
                }
                pegi.newLine();
            }
            pegi.newLine();

            changed |= dic.newElement_PEGI();

            return changed;
        }

        public static bool newElement_PEGI(this Dictionary<int, string> dic)
        {
            bool changed = false;
            pegi.newLine();
            pegi.write("______New [Key, Value]");
            pegi.newLine();
            changed |= pegi.edit(ref newEnumKey); changed |= pegi.edit(ref newEnumName);
            string dummy;
            bool isNewIndex = !dic.TryGetValue(newEnumKey, out dummy);
            bool isNewValue = !dic.ContainsValue(newEnumName);

            if ((isNewIndex) && (isNewValue) && (icon.Add.Click("Add Element", 25)))
            {
                dic.Add(newEnumKey, newEnumName);
                changed = true;
                SetNewKeyToMax(dic);
                newEnumName = "UNNAMED";
            }

            if (!isNewIndex)
                pegi.write("Index Takken by " + dummy);
            else if (!isNewValue)
                pegi.write("Value already assigned ");

            pegi.newLine();

            return changed;
        }
        public static void SetNewKeyToMax(Dictionary<int, string> dic)
        {
            newEnumKey = 1;
            string dummy;
            while (dic.TryGetValue(newEnumKey, out dummy)) newEnumKey++;
        }

        /*
        class TypeSwitch
        {
            Dictionary<Type, Action<object>> matches = new Dictionary<Type, Action<object>>();
            public TypeSwitch Case<T>(Action<T> action) { matches.Add(typeof(T), (x) => action((T)x)); return this; }
            public void Switch(object x, string name) { matches[x.GetType()](x); }
        }*/

        /*
                static List<MonoBehaviour> editorSubscribedMb = new List<MonoBehaviour>();
#if UNITY_EDITOR

                public static bool SubscribeToEditorUpdate_PEGI<T>(this T mb, EditorApplication.CallbackFunction myMethodName) where T : MonoBehaviour
                {

                    if (!editorSubscribedMb.Contains(mb))
                    {
                        if (icon.Play.Click())
                        {
                            EditorApplication.update += myMethodName;
                            editorSubscribedMb.Add(mb);
                            return true;
                        }

                    }
                    else
                    {
                        if ("Stop Updates".Click())
                        {
                            EditorApplication.update -= myMethodName;
                            editorSubscribedMb.Remove(mb);
                            return true;
                        }

                    }




                    return false;
                }
#else
                public static bool SubscribeToEditorUpdate_PEGI<T>(this T mb, Action myMethodName) where T : MonoBehaviour
                {
                    return false;
                }
#endif

                public static bool isSubscribedToEditorUpdates(this MonoBehaviour mb)
                {
                    return editorSubscribedMb.Contains(mb);
                }

            }*/
    }
}

#endif