#set page(
  paper: "a4",
  header: align(right)[
    #image("TEKO-logo-pink.svg", height: 24pt)
  ]
)
#set text(lang: "de")
#set align(center)

#v(1cm)

#text("TEKO Schweizerische Fachschule", size: 16pt, weight: "bold")
#v(1cm)
#text("Informatiker HF Applikationsentwicklung", size: 13pt, weight: "bold")
#v(0.5cm)
#line(length: 100%)
#v(0.1cm)
#text("Diplomarbeit 2026", size: 19pt, weight: "bold")
#v(0.1cm)
#text("IDE-Erweiterung zentraler Entwicklungsprozesse", size: 19pt, weight: "bold")
#v(0.4cm)
#line(length: 100%)

// Bei vertraulichen Arbeiten muss der Vermerk «vertraulich» auf der Titelseite sichtbar sein.
// #v(0.5cm)
// #text("vertraulich", size: 14pt, weight: "bold")

#v(3cm)

#pad(x: 80pt)[
  #grid(
    columns: (1fr, 1fr),
    gutter: 0pt,
    align(left)[
      _Autor:_\
      Adrian Aeschlimann\
      _Klasse:_\
      B-TIA-23-T
    ],
    align(right)[
      _Betreuende Person:_\
      Stefan Canobbio\
      _Experte/Expertin:_\
      TODO
    ]
  )
]

#v(3cm)

#text("Abgabedatum: 02.11.2026")\
#text("Version 0.1")
