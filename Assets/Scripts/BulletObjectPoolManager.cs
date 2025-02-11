using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public class BulletObjectPoolManager : MonoBehaviour
{
    [Header("�I�u�W�F�N�g�v�[���̐ݒ�")]
    [SerializeField] private int _initCount = 50;
    [SerializeField] private int _maxCount = 200;
    [SerializeField] private EnumToObjectDatabase _objectDatabase;

    private Dictionary<BulletTypeEnum, ObjectPool<BulletAttackBase>> _objectPoolDict;
    private void Start()
    {
        InitPool();
    }
    private void InitPool()
    {
        var mappings = _objectDatabase.Mappings;
        foreach (var pair in mappings)
        {
            _objectPoolDict[pair.Type] = new ObjectPool<BulletAttackBase>(
                () =>
                {
                    // pair�ɉ������v���n�u���C���X�^���X���������̂Ń����_����p����B
                    var bullet = Instantiate(_objectDatabase.GetGameObject(pair.Type), transform);
                    var component = bullet.GetComponent<BulletAttackBase>();
                    component.OnReturnToPool += () => _objectPoolDict[pair.Type].Release(component);
                    return component;
                },
                OnGetFromPool, OnReleaseToPool, OnDisposePoolObject,
                true, _initCount, _maxCount
                );
            // �v�[���𖞂����Ă������ߗ\�ߐ������Ă���
            var list = new List<BulletAttackBase>();
            for (int i = 0; i < _initCount; i++)
            {
                var component = _objectPoolDict[pair.Type].Get();
                list.Add(component);
            }
            foreach (var component in list)
            {
                _objectPoolDict[pair.Type].Release(component);
            }
        }
    }
    private void OnGetFromPool(BulletAttackBase parameter) => parameter.gameObject.SetActive(true);
    private void OnReleaseToPool(BulletAttackBase parameter) => parameter.gameObject.SetActive(false);
    private void OnDisposePoolObject(BulletAttackBase parameter) => Destroy(parameter.gameObject);

    public BulletAttackBase Get(BulletTypeEnum type) => _objectPoolDict[type].Get();
    public void Release(BulletTypeEnum type, BulletAttackBase component) => _objectPoolDict[type].Release(component);

}
