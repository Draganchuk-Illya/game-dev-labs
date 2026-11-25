using UnityEngine;
using UnityEngine.UIElements;

// Інтерфейс гри: смужка здоров'я у верхньому лівому куті.
public class UIHandler : MonoBehaviour
{
    // один екземпляр на сцену, щоб до інтерфейсу можна було звернутись з будь-якого скрипта
    public static UIHandler instance { get; private set; }

    private VisualElement m_Healthbar;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        m_Healthbar = uiDocument.rootVisualElement.Q<VisualElement>("Healthbar");
        SetHealthValue(1.0f);

        // вікно діалогу, лічильник і фінальний екран знадобляться в наступних роботах, поки ховаю їх
        Hide(uiDocument, "NPCDialogue");
        Hide(uiDocument, "RobotCounter");
        Hide(uiDocument, "EndScreen");
    }

    void Hide(UIDocument uiDocument, string elementName)
    {
        VisualElement element = uiDocument.rootVisualElement.Q<VisualElement>(elementName);
        if (element != null)
        {
            element.style.display = DisplayStyle.None;
        }
    }

    // percentage від 0 до 1
    public void SetHealthValue(float percentage)
    {
        m_Healthbar.style.width = Length.Percent(100 * percentage);
    }
}
