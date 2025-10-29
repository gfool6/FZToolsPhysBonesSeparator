using System.Diagnostics;
using System.Collections.Specialized;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;
using EUI = FZTools.EditorUtils.UI;
using ELayout = FZTools.EditorUtils.Layout;
using static FZTools.AvatarUtils;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.Dynamics;

namespace FZTools
{
    public class FZPhysBonesSeparator : EditorWindow
    {
        [SerializeField] GameObject targetAvatar;

        List<VRCPhysBone> physBones = new List<VRCPhysBone>();
        List<VRCPhysBoneCollider> physBoneColliders = new List<VRCPhysBoneCollider>();
        List<ContactBase> physBoneContacts = new List<ContactBase>();

        [MenuItem("FZTools/PhysBonesSeparator(β)")]
        private static void OpenWindow()
        {
            var window = GetWindow<FZPhysBonesSeparator>();
            window.titleContent = new GUIContent("PhysBonesSeparator(β)");
        }

        private void OnGUI()
        {
            ELayout.Horizontal(() =>
            {
                EUI.Space();
                ELayout.Vertical(() =>
                {
                    var text = "以下の機能を提供します\n"
                            + "・アバターのArmature以下に設定されたPhysBonesを分離してPrefab化します\n"
                            + "・分離したPrefabについては、ルートの下にPB・Collider・Contactと分けられ、\n"
                            + "　その下にそれぞれのコンポーネントが入ります\n";
                    EUI.InfoBox(text);

                    EUI.Label("Target Avatar");
                    EUI.ChangeCheck(
                        () => EUI.ObjectField<GameObject>(ref targetAvatar),
                        () =>
                        {
                        });
                    EUI.Space();

                    EUI.Space(2);
                    using (new EditorGUI.DisabledScope(targetAvatar == null))
                    {
                        EUI.Button("作成", Separate);
                    }
                });
            });
        }

        private void Separate()
        {
            // TODO
            // アバターと服で処理分ける
            // AnimatorからArmature拾える場合はAnimatorから探す
            // そうでない場合はなんかいい感じに…名前で探すか…？　服もこれできたらいいんだけどなぁ…

            if (targetAvatar == null)
            {
                UnityEngine.Debug.LogError("Target Avatarが設定されていません");
                return;
            }

            physBones = targetAvatar.GetComponentsInChildren<VRCPhysBone>(true).ToList();
            physBoneColliders = targetAvatar.GetComponentsInChildren<VRCPhysBoneCollider>(true).ToList();
            physBoneContacts = targetAvatar.GetComponentsInChildren<ContactBase>(true).ToList();

            // PB分離Prefab用Object作成
            var dynamicsObj = new GameObject("AvatarDynamics");
            dynamicsObj.transform.SetParent(targetAvatar.transform);

            CreatePBC(dynamicsObj);
            CreatePB(dynamicsObj);
            CreateContact(dynamicsObj);

            var prefabOutputRootPath = $"{AssetUtils.OutputRootPath(targetAvatar.name)}/Prefab";
            AssetUtils.CreateDirectoryRecursive(prefabOutputRootPath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(dynamicsObj, $"{prefabOutputRootPath}/{targetAvatar.name}_Dynamics.prefab", InteractionMode.AutomatedAction);

            physBones.ForEach(pb => DestroyImmediate(pb));
            physBoneColliders.ForEach(pbc => DestroyImmediate(pbc));
            physBoneContacts.ForEach(contact => DestroyImmediate(contact));
        }

        void CreatePB(GameObject rootObj)
        {
            var pbRoot = new GameObject("PB");
            pbRoot.transform.SetParent(rootObj.transform);
            physBones.ForEach(pb =>
            {
                var pbObj = new GameObject(pb.name);
                pbObj.transform.SetParent(pbRoot.transform);
                UnityEditorInternal.ComponentUtility.CopyComponent(pb);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(pbObj);

                VRCPhysBone newPB = pbObj.GetComponent<VRCPhysBone>();
                if (newPB.rootTransform == null)
                {
                    newPB.rootTransform = pb.transform;
                }

                var pbcRoot = rootObj.transform.Find("PBC");
                if (newPB.colliders != null && newPB.colliders.Count() > 0)
                {
                    List<VRCPhysBoneColliderBase> newColliders = new List<VRCPhysBoneColliderBase>();
                    foreach (var collider in newPB.colliders)
                    {
                        if (collider == null) continue;

                        var foundCollider = pbcRoot.GetComponentsInChildren<VRCPhysBoneCollider>(true).FirstOrDefault(x => x.name == collider.name);
                        if (foundCollider != null)
                        {
                            newColliders.Add(foundCollider);
                        }
                    }
                    newPB.colliders = newColliders;
                }
                pb.enabled = false;
            });
        }

        void CreatePBC(GameObject rootObj)
        {
            var pbcRoot = new GameObject("PBC");
            pbcRoot.transform.SetParent(rootObj.transform);
            physBoneColliders.ForEach(pbc =>
            {
                var pbcObj = new GameObject(pbc.name);
                pbcObj.transform.SetParent(pbcRoot.transform);
                UnityEditorInternal.ComponentUtility.CopyComponent(pbc);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(pbcObj);

                VRCPhysBoneCollider newPBC = pbcObj.GetComponent<VRCPhysBoneCollider>();
                if (newPBC.rootTransform == null)
                {
                    newPBC.rootTransform = pbc.transform;
                }
                pbc.enabled = false;
            });
        }

        void CreateContact(GameObject rootObj)
        {
            var contactRoot = new GameObject("Contacts");
            contactRoot.transform.SetParent(rootObj.transform);
            physBoneContacts.ForEach(contact =>
            {
                var contactObj = new GameObject(contact.name);
                contactObj.transform.SetParent(contactRoot.transform);
                UnityEditorInternal.ComponentUtility.CopyComponent(contact);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(contactObj);
                contact.enabled = false;
            });
        }
    }
}