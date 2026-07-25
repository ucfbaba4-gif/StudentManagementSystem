using Microsoft.AspNetCore.Mvc;
using StudentManagement.Models;

namespace StudentManagement.Controllers;

public class StudentController : Controller
{
    // Simulation d'une base de données
    private static List<Student> students = new List<Student>()
    {
        new Student { Id = 1, Name = "Ali", Age = 20 },
        new Student { Id = 2, Name = "Sara", Age = 22 },
        new Student { Id = 3, Name = "Hatim", Age = 21 }
    };

    // ==========================
    // READ
    // ==========================
    public IActionResult Index()
    {
        return View(students);
    }

    // ==========================
    // CREATE (GET)
    // ==========================
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // ==========================
    // CREATE (POST)
    // ==========================
    [HttpPost]
    public IActionResult Create(Student student)
    {
        // Vérifie si la liste est vide
        if (students.Count == 0)
        {
            student.Id = 1;
        }
        else
        {
            student.Id = students.Max(s => s.Id) + 1;
        }

        students.Add(student);

        return RedirectToAction("Index");
    }

    // ==========================
    // EDIT (GET)
    // ==========================
    [HttpGet]
    public IActionResult Edit(int id)
    {
        Student? student = students.FirstOrDefault(s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // ==========================
    // EDIT (POST)
    // ==========================
    [HttpPost]
    public IActionResult Edit(Student student)
    {
        Student? existingStudent = students.FirstOrDefault(s => s.Id == student.Id);

        if (existingStudent == null)
        {
            return NotFound();
        }

        existingStudent.Name = student.Name;
        existingStudent.Age = student.Age;

        return RedirectToAction("Index");
    }

    // ==========================
    // DETAILS
    // ==========================
    public IActionResult Details(int id)
    {
        Student? student = students.FirstOrDefault(s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // ==========================
    // DELETE
    // ==========================
    public IActionResult Delete(int id)
    {
        Student? student = students.FirstOrDefault(s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        students.Remove(student);

        return RedirectToAction("Index");
    }
}